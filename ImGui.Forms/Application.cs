using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.SDL3;
using Hexa.NET.SDL3;
using ImGui.Forms.Extensions;
using ImGui.Forms.Factories;
using ImGui.Forms.Localization;
using ImGui.Forms.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImSDLEvent = Hexa.NET.ImGui.Backends.SDL3.SDLEvent;
using ImSDLGPUCommandBuffer = Hexa.NET.ImGui.Backends.SDL3.SDLGPUCommandBuffer;
using ImSDLGPUDevice = Hexa.NET.ImGui.Backends.SDL3.SDLGPUDevice;
using ImSDLGPURenderPass = Hexa.NET.ImGui.Backends.SDL3.SDLGPURenderPass;
using ImSDLWindow = Hexa.NET.ImGui.Backends.SDL3.SDLWindow;
using Rectangle = ImGui.Forms.Support.Rectangle;
using SDLEvent = Hexa.NET.SDL3.SDLEvent;
using SDLGPUCommandBuffer = Hexa.NET.SDL3.SDLGPUCommandBuffer;
using SDLGPUDevice = Hexa.NET.SDL3.SDLGPUDevice;
using SDLGPURenderPass = Hexa.NET.SDL3.SDLGPURenderPass;
using SDLWindow = Hexa.NET.SDL3.SDLWindow;
using SDLWindowPtr = Hexa.NET.SDL3.SDLWindowPtr;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace ImGui.Forms;

public class Application
{
    private bool _isClosing;
    private bool _shouldClose;
    private unsafe SDLGPUTexture* _depthTexture;
    private int _depthTextureWidth;
    private int _depthTextureHeight;
    private SDLGPUTextureFormat _depthFormat;

    private ExecutionContext? _executionContext;
    private readonly List<GpuPrepareAction> _gpuPrepareActions = [];
    private readonly List<GpuRenderAction> _gpuRenderActions = [];

    private readonly List<DragDropEvent> _dragDropEvents = [];

    #region Static properties

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static Application Instance { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    #endregion

    #region Properties

    public Form? MainForm => _executionContext?.MainForm;

    internal SDLWindowPtr? Window => _executionContext?.Window;
    internal ImageFactory? Images => _executionContext?.Images;
    internal IdFactory? Ids => _executionContext?.Ids;
    internal unsafe SDLGPUDevice* GpuDevice => _executionContext == null ? (SDLGPUDevice*)0 : _executionContext.GpuDevice;
    internal SDLGPUTextureFormat SwapchainFormat => _executionContext?.SwapchainFormat ?? default;

    public ILocalizer? Localizer { get; private set; }

    #endregion

    #region Events

    public event EventHandler<Exception?>? UnhandledException;

    #endregion

    public Application(ILocalizer? localizer = null)
    {
        Localizer = localizer;
        Instance = this;

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public unsafe void Execute(Form form)
    {
        if (_executionContext != null)
            throw new InvalidOperationException("There already is an application running.");

        if (!SDL.Init((int)(SDLInitFlags.Video | SDLInitFlags.Gamepad)))
        {
            Console.WriteLine($"Error: SDL_Init(): {SDL.GetErrorS()}");
            return;
        }

        var windowFlags = SDLWindowFlags.Resizable | SDLWindowFlags.Hidden | SDLWindowFlags.HighPixelDensity;
        SDLWindow* window = SDL.CreateWindow(form.Title, form.Width, form.Height, (ulong)windowFlags);
        if (window == null)
        {
            Console.WriteLine($"Error: SDL_CreateWindow(): {SDL.GetErrorS()}");
            return;
        }

        SDL.SetWindowPosition(window, 50, 70);
        SDL.ShowWindow(window);

        SDLGPUDevice* gpuDevice = CreateGpuDeviceInPreferredOrder();
        if (gpuDevice == null)
        {
            Console.WriteLine("Error: SDL_CreateGPUDevice(): failed for all preferred drivers (vulkan, metal, direct3d).");
            return;
        }

        if (!SDL.ClaimWindowForGPUDevice(gpuDevice, window))
        {
            Console.WriteLine($"Error: SDL_ClaimWindowForGPUDevice(): {SDL.GetErrorS()}");
            return;
        }

        SDL.SetGPUSwapchainParameters(gpuDevice, window, SDLGPUSwapchainComposition.Sdr, SDLGPUPresentMode.Mailbox);

        var ctx = Hexa.NET.ImGui.ImGui.CreateContext();
        Hexa.NET.ImGui.ImGui.SetCurrentContext(ctx);

        ImGuiIOPtr io = Hexa.NET.ImGui.ImGui.GetIO();
        io.IniFilename = null;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard
                          | ImGuiConfigFlags.NavEnableGamepad;
        io.ConfigErrorRecoveryEnableAssert = false;
        io.ConfigErrorRecovery = false;
        io.ConfigErrorRecoveryEnableDebugLog = false;
        io.ConfigErrorRecoveryEnableTooltip = false;

        io.ConfigDpiScaleFonts = true;
        io.ConfigDpiScaleViewports = true;

        ImGuiPlatformIOPtr platformIo = Hexa.NET.ImGui.ImGui.GetPlatformIO();
        platformIo.PlatformGetClipboardTextFn = (void*)Marshal.GetFunctionPointerForDelegate(Sdl2NativeExtensions.GetClipboardText);
        platformIo.PlatformSetClipboardTextFn = (void*)Marshal.GetFunctionPointerForDelegate(Sdl2NativeExtensions.SetClipboardText);

        _executionContext = new ExecutionContext(form, window, new ImageFactory(gpuDevice), new IdFactory(), gpuDevice, SDL.GetGPUSwapchainTextureFormat(gpuDevice, window));
        FontFactory.Initialize(io);

        ImGuiImplSDL3.SetCurrentContext(ctx);
        ImGuiImplSDL3.InitForSDLGPU((ImSDLWindow*)window);

        ImGuiImplSDLGPU3InitInfo initInfo = new()
        {
            Device = (ImSDLGPUDevice*)gpuDevice,
            ColorTargetFormat = (int)SDL.GetGPUSwapchainTextureFormat(gpuDevice, window),
            MSAASamples = (int)SDLGPUSampleCount.Samplecount1
        };
        ImGuiImplSDL3.SDLGPU3Init(&initInfo);

        ImGuiSampler.Initialize(gpuDevice);

        var shouldInvokeLoad = false;

        while (!_shouldClose)
        {
            UpdateApplicationEvents();

            _executionContext.Images.FreeTextures();

            SDL.StartTextInput(window);

            SDLEvent e;
            while (SDL.PollEvent(&e))
            {
                ImGuiImplSDL3.ProcessEvent((ImSDLEvent*)&e);

                if (e.Window.WindowID != SDL.GetWindowID(window))
                    continue;

                switch ((SDLEventType)e.Type)
                {
                    case SDLEventType.WindowCloseRequested:
                        if (!ShouldCancelClose())
                            _shouldClose = true;
                        break;

                    case SDLEventType.Quit:
                        if (!_isClosing)
                            _shouldClose = true;
                        break;

                    case SDLEventType.WindowShown:
                        shouldInvokeLoad = true;
                        break;

                    case SDLEventType.WindowResized:
                        int w = 0, h = 0;
                        SDL.GetWindowSize(window, ref w, ref h);

                        form.Size = new Vector2(w, h);
                        form.OnResized();
                        break;

                    case SDLEventType.DropFile:
                        var dropEvent = Unsafe.Read<SDLDropEvent>(&e);

                        string? file = dropEvent.Data == null ? null : Marshal.PtrToStringUTF8((nint)dropEvent.Data);
                        Window_DragDrop(file);
                        break;
                }
            }

            if (((SDLWindowFlags)SDL.GetWindowFlags(window) & SDLWindowFlags.Minimized) != 0)
            {
                SDL.Delay(10);
                continue;
            }

            ImGuiImplSDL3.SDLGPU3NewFrame();
            ImGuiImplSDL3.NewFrame();
            Hexa.NET.ImGui.ImGui.NewFrame();

            if (shouldInvokeLoad)
            {
                form.OnLoad();
                shouldInvokeLoad = false;
            }

            // Render Form
            form.Update();

            Hexa.NET.ImGui.ImGui.Render();
            ImDrawData* drawData = Hexa.NET.ImGui.ImGui.GetDrawData();
            bool isMinimized = drawData->DisplaySize.X <= 0 || drawData->DisplaySize.Y <= 0;

            SDLGPUCommandBuffer* commandBuffer = SDL.AcquireGPUCommandBuffer(gpuDevice);
            SDLGPUTexture* swapTexture;

            if (!SDL.WaitAndAcquireGPUSwapchainTexture(commandBuffer, window, &swapTexture, null, null))
                continue;

            if (swapTexture != null && !isMinimized)
            {
                ImGuiImplSDL3.SDLGPU3PrepareDrawData(drawData, (ImSDLGPUCommandBuffer*)commandBuffer);
                ExecuteQueuedGpuPrepares(gpuDevice, commandBuffer);

                int depthWidth = Math.Max(1, (int)(drawData->DisplaySize.X * drawData->FramebufferScale.X));
                int depthHeight = Math.Max(1, (int)(drawData->DisplaySize.Y * drawData->FramebufferScale.Y));
                EnsureDepthTarget(gpuDevice, depthWidth, depthHeight);

                SDLGPUColorTargetInfo targetInfo = new()
                {
                    Texture = swapTexture,
                    ClearColor = new SDLFColor
                    {
                        R = .45f,
                        G = .55f,
                        B = .60f,
                        A = 1f
                    },
                    LoadOp = SDLGPULoadOp.Clear,
                    StoreOp = SDLGPUStoreOp.Store,
                    MipLevel = 0,
                    LayerOrDepthPlane = 0,
                    Cycle = 0
                };

                SDLGPUDepthStencilTargetInfo depthTargetInfo = new()
                {
                    Texture = _depthTexture,
                    ClearDepth = 1f,
                    LoadOp = SDLGPULoadOp.Clear,
                    StoreOp = SDLGPUStoreOp.Store,
                    StencilLoadOp = SDLGPULoadOp.DontCare,
                    StencilStoreOp = SDLGPUStoreOp.DontCare,
                    Cycle = 0
                };

                SDLGPURenderPass* renderPass = SDL.BeginGPURenderPass(commandBuffer, &targetInfo, 1, &depthTargetInfo);
                ImGuiImplSDL3.SDLGPU3RenderDrawData(drawData, (ImSDLGPUCommandBuffer*)commandBuffer, (ImSDLGPURenderPass*)renderPass, null);
                ExecuteQueuedGpuRenders(gpuDevice, commandBuffer, renderPass);
                SDL.EndGPURenderPass(renderPass);
            }

            SDL.SubmitGPUCommandBuffer(commandBuffer);
        }

        SDL.WaitForGPUIdle(gpuDevice);
        ImGuiImplSDL3.Shutdown();
        ImGuiImplSDL3.SDLGPU3Shutdown();
        Hexa.NET.ImGui.ImGui.DestroyContext();

        FontFactory.Dispose();
        _executionContext.Images.Dispose();

        ImGuiSampler.Release(gpuDevice);
        DestroyDepthTarget(gpuDevice);

        SDL.ReleaseWindowFromGPUDevice(gpuDevice, window);
        SDL.StopTextInput(window);
        SDL.DestroyGPUDevice(gpuDevice);
        SDL.DestroyWindow(window);
        SDL.Quit();
    }

    public void Exit()
    {
        if (_executionContext == null)
            throw new InvalidOperationException("There is no application running.");

        _shouldClose = true;
    }

    public void SetSize(Vector2 size)
    {
        if (_executionContext == null)
            throw new InvalidOperationException("There is no application running.");

        _executionContext.MainForm.Size = size;

        SDL.SetWindowSize(_executionContext.Window, (int)size.X, (int)size.Y);
    }

    private void UpdateApplicationEvents()
    {
        UpdateDragDropEvents();

        _gpuPrepareActions.Clear();
        _gpuRenderActions.Clear();
    }

    private void UpdateDragDropEvents()
    {
        if (_dragDropEvents.Count <= 0)
            return;

        if (_dragDropEvents.All(x => !x.IsInvalid(_executionContext!.MainForm.Size)))
        {
            _dragDropEvents.Clear();
            return;
        }

        foreach (var dragDropEvent in _dragDropEvents.ToArray())
        {
            if (!dragDropEvent.IsInvalid(_executionContext!.MainForm.Size))
            {
                _dragDropEvents.Remove(dragDropEvent);
                continue;
            }

            // For some reason, mouse positions get stuck at float.MinValue when the window is inactive.
            // DragFile events are registered even on inactive windows, so this updates to a valid mouse
            // position as soon as available before throwing the DropFile event away.
            dragDropEvent.MousePosition = Hexa.NET.ImGui.ImGui.GetMousePos();
        }
    }

    internal void EnqueueGpuPrepareAction(GpuPrepareAction action)
    {
        _gpuPrepareActions.Add(action);
    }

    internal void EnqueueGpuRenderAction(GpuRenderAction action)
    {
        _gpuRenderActions.Add(action);
    }

    private unsafe void ExecuteQueuedGpuPrepares(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer)
    {
        foreach (var action in _gpuPrepareActions)
            action(gpuDevice, commandBuffer);
    }

    private unsafe void ExecuteQueuedGpuRenders(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer, SDLGPURenderPass* renderPass)
    {
        foreach (var action in _gpuRenderActions)
            action(gpuDevice, commandBuffer, renderPass);
    }

    #region Window event

    private bool ShouldCancelClose()
    {
        if (_executionContext == null)
            return false;

        // If any close blocking modal is open, cancel closing
        if (_executionContext.MainForm.HasBlockingModals())
            return true;

        // If not closing action is currently taking place, start closing action
        if (!_isClosing && !_shouldClose)
        {
            _isClosing = true;
            IsClosing();
        }

        // Determine, if closing action was cancelled
        return _isClosing || !_shouldClose;
    }

    // ReSharper disable once AsyncVoidMethod
    private async void IsClosing()
    {
        if (_executionContext == null)
            return;

        var args = new ClosingEventArgs();
        await _executionContext.MainForm.OnClosing(args);

        _isClosing = false;
        _shouldClose = !args.Cancel;
    }

    private void Window_DragDrop(string? path)
    {
        if (path == null)
            return;

        _dragDropEvents.Add(new DragDropEvent(path, Hexa.NET.ImGui.ImGui.GetMousePos(), false));
    }

    #endregion

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        UnhandledException?.Invoke(this, e.ExceptionObject as Exception);
    }

    private static unsafe SDLGPUDevice* CreateGpuDeviceInPreferredOrder()
    {
        uint shaderFormats = (uint)(SDLGPUShaderFormat.Spirv | SDLGPUShaderFormat.Dxil | SDLGPUShaderFormat.Metallib);
        ReadOnlySpan<string> preferredDrivers = ["vulkan", "metal", "direct3d12"];

        foreach (string driver in preferredDrivers)
        {
            SDLGPUDevice* gpuDevice = SDL.CreateGPUDevice(shaderFormats, false, driver);
            if (gpuDevice != null)
            {
                Console.WriteLine($"SDL_CreateGPUDevice(): using '{driver}' driver.");
                return gpuDevice;
            }

            Console.WriteLine($"SDL_CreateGPUDevice(): failed for '{driver}' driver: {SDL.GetErrorS()}");
        }

        return null;
    }

    private unsafe void EnsureDepthTarget(SDLGPUDevice* gpuDevice, int width, int height)
    {
        if (_depthTexture != null && _depthTextureWidth == width && _depthTextureHeight == height)
            return;

        DestroyDepthTarget(gpuDevice);

        _depthFormat = SelectDepthFormat(gpuDevice);
        _depthTexture = SDL.CreateGPUTexture(gpuDevice, new SDLGPUTextureCreateInfo
        {
            Width = (uint)width,
            Height = (uint)height,
            Format = _depthFormat,
            Type = SDLGPUTextureType.Texturetype2D,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDLGPUSampleCount.Samplecount1,
            Usage = (uint)SDLGPUTextureUsageFlags.DepthStencilTarget
        });

        _depthTextureWidth = width;
        _depthTextureHeight = height;
    }

    private unsafe void DestroyDepthTarget(SDLGPUDevice* gpuDevice)
    {
        if (_depthTexture == null)
            return;

        SDL.ReleaseGPUTexture(gpuDevice, _depthTexture);
        _depthTexture = null;
        _depthTextureWidth = 0;
        _depthTextureHeight = 0;
    }

    private static unsafe SDLGPUTextureFormat SelectDepthFormat(SDLGPUDevice* gpuDevice)
    {
        SDLGPUTextureType textureType = SDLGPUTextureType.Texturetype2D;
        uint usage = (uint)SDLGPUTextureUsageFlags.DepthStencilTarget;

        if (SDL.GPUTextureSupportsFormat(gpuDevice, SDLGPUTextureFormat.D32Float, textureType, usage))
            return SDLGPUTextureFormat.D32Float;

        if (SDL.GPUTextureSupportsFormat(gpuDevice, SDLGPUTextureFormat.D24Unorm, textureType, usage))
            return SDLGPUTextureFormat.D24Unorm;

        throw new InvalidOperationException("No supported depth format was found for this GPU backend.");
    }

    internal bool TryGetDragDrop(Rectangle controlRect, out string[] files)
    {
        files = new string[_dragDropEvents.Count];
        var index = 0;

        foreach (var dragDropEvent in _dragDropEvents)
        {
            if (dragDropEvent.IsHandled || dragDropEvent.IsInvalid(_executionContext!.MainForm.Size))
                continue;

            if (!controlRect.Contains(dragDropEvent.MousePosition))
                continue;

            files[index++] = dragDropEvent.File;
            dragDropEvent.IsHandled = true;
        }

        Array.Resize(ref files, index);
        return files.Length > 0;
    }
}

internal unsafe delegate void GpuPrepareAction(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer);
internal unsafe delegate void GpuRenderAction(SDLGPUDevice* gpuDevice, SDLGPUCommandBuffer* commandBuffer, SDLGPURenderPass* renderPass);

internal sealed unsafe class ExecutionContext(Form mainForm, SDLWindowPtr window, ImageFactory images, IdFactory ids, SDLGPUDevice* gpuDevice, SDLGPUTextureFormat swapchainFormat)
{
    public Form MainForm { get; } = mainForm;
    public SDLWindowPtr Window { get; } = window;
    public ImageFactory Images { get; } = images;
    public IdFactory Ids { get; } = ids;
    public SDLGPUDevice* GpuDevice { get; } = gpuDevice;
    public SDLGPUTextureFormat SwapchainFormat { get; } = swapchainFormat;
}

internal class DragDropEvent(string file, Vector2 mousePosition, bool isHandled)
{
    private static readonly Vector2 InvalidVector = new(float.MinValue);

    public string File { get; } = file;
    public Vector2 MousePosition { get; set; } = mousePosition;
    public bool IsHandled { get; set; } = isHandled;

    public bool IsInvalid(Vector2 windowSize) => MousePosition == InvalidVector
                                                 || MousePosition.X < 0 || MousePosition.X > windowSize.X
                                                 || MousePosition.Y < 0 || MousePosition.Y > windowSize.Y;
}