using System;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.SDL3;
using Hexa.NET.SDL3;
using System.Numerics;
using ImGui.Forms.Factories;
using ImGui.Forms.Localization;
using ImSDLEvent = Hexa.NET.ImGui.Backends.SDL3.SDLEvent;
using ImSDLWindow = Hexa.NET.ImGui.Backends.SDL3.SDLWindow;
using SDLWindow = Hexa.NET.SDL3.SDLWindow;
using SDLEvent = Hexa.NET.SDL3.SDLEvent;
using SDLGPUDevice = Hexa.NET.SDL3.SDLGPUDevice;
using ImSDLGPUDevice = Hexa.NET.ImGui.Backends.SDL3.SDLGPUDevice;
using SDLGPUCommandBuffer = Hexa.NET.SDL3.SDLGPUCommandBuffer;
using ImSDLGPUCommandBuffer = Hexa.NET.ImGui.Backends.SDL3.SDLGPUCommandBuffer;
using SDLGPURenderPass = Hexa.NET.SDL3.SDLGPURenderPass;
using ImSDLGPURenderPass = Hexa.NET.ImGui.Backends.SDL3.SDLGPURenderPass;
using SDLWindowPtr = Hexa.NET.SDL3.SDLWindowPtr;

namespace ImGui.Forms;

public class Application
{
    private bool _isClosing;
    private bool _shouldClose;

    private ExecutionContext _executionContext;

    //private DragDropEventEx[] _dragDropEvents;
    private bool[] _frameHandledDragDrops;

    #region Static properties

    public static Application Instance { get; private set; }

    #endregion

    #region Properties

    public Form MainForm => _executionContext.MainForm;

    internal SDLWindowPtr Window => _executionContext.Window;
    internal ImageFactory Images => _executionContext.Images;
    internal IdFactory Ids => _executionContext.Ids;

    public ILocalizer? Localizer { get; set; }

    #endregion

    #region Events

    public event EventHandler<Exception> UnhandledException;

    #endregion

    public Application(ILocalizer? localizer = null)
    {
        Localizer = localizer;
        Instance = this;

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public unsafe void Execute(Form form)
    {
        if (!SDL.Init((int)(SDLInitFlags.Video | SDLInitFlags.Gamepad)))
        {
            Console.WriteLine($"Error: SDL_Init(): {SDL.GetErrorS()}");
            return;
        }

        float mainScale = SDL.GetDisplayContentScale(SDL.GetPrimaryDisplay());
        var windowFlags = SDLWindowFlags.Resizable | SDLWindowFlags.Hidden | SDLWindowFlags.HighPixelDensity;
        SDLWindow* window = SDL.CreateWindow(form.Title, (int)(form.Width * mainScale), (int)(form.Height * mainScale), (ulong)windowFlags);
        if (window == null)
        {
            Console.WriteLine($"Error: SDL_CreateWindow(): {SDL.GetErrorS()}");
            return;
        }

        SDL.SetWindowPosition(window, 50, 70);
        SDL.ShowWindow(window);

        SDLGPUDevice* gpuDevice = SDL.CreateGPUDevice((uint)(SDLGPUShaderFormat.Spirv | SDLGPUShaderFormat.Dxil | SDLGPUShaderFormat.Metallib), true, (byte*)null);
        if (gpuDevice == null)
        {
            Console.WriteLine($"Error: SDL_CreateGPUDevice(): {SDL.GetErrorS()}");
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
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard
                        | ImGuiConfigFlags.NavEnableGamepad;

        io.ConfigDpiScaleFonts = true;
        io.ConfigDpiScaleViewports = true;

        _executionContext = new ExecutionContext(form, window, new ImageFactory(gpuDevice), new IdFactory());
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

        bool done = false;
        while (!done)
        {
            UpdateApplicationEvents();

            Images.FreeTextures();
            Ids.FreeUnused();

            SDLEvent e;
            while (SDL.PollEvent(&e))
            {
                ImGuiImplSDL3.ProcessEvent((ImSDLEvent*)&e);
                var type = (SDLEventType)e.Type;
                if (type == SDLEventType.Quit ||
                    (type == SDLEventType.WindowCloseRequested &&
                     e.Window.WindowID == SDL.GetWindowID(window)))
                {
                    done = true;
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

                SDLGPURenderPass* renderPass = SDL.BeginGPURenderPass(commandBuffer, &targetInfo, 1, null);
                ImGuiImplSDL3.SDLGPU3RenderDrawData(drawData, (ImSDLGPUCommandBuffer*)commandBuffer, (ImSDLGPURenderPass*)renderPass, null);
                SDL.EndGPURenderPass(renderPass);
            }

            SDL.SubmitGPUCommandBuffer(commandBuffer);
        }

        SDL.WaitForGPUIdle(gpuDevice);
        ImGuiImplSDL3.Shutdown();
        ImGuiImplSDL3.SDLGPU3Shutdown();
        Hexa.NET.ImGui.ImGui.DestroyContext();

        FontFactory.Dispose();

        SDL.ReleaseWindowFromGPUDevice(gpuDevice, window);
        SDL.DestroyGPUDevice(gpuDevice);
        SDL.DestroyWindow(window);
        SDL.Quit();
    }

    //public void Execute(Form form)
    //{
    //    if (Instance != null)
    //        throw new InvalidOperationException("There already is an application running.");

    //    CreateApplication(form);

    //    _executionContext.Window.Resized += Window_Resized;
    //    _executionContext.Window.DragDrop += Window_DragDrop;
    //    _executionContext.Window.Shown += Window_Shown;
    //    _executionContext.Window.SetCloseRequestedHandler(ShouldCancelClose);

    //    var cl = _executionContext.GraphicsDevice.ResourceFactory.CreateCommandList();

    //    // Main application loop
    //    while (_executionContext.Window.Exists)
    //    {
    //        if (!UpdateFrame(cl))
    //            break;
    //    }

    //    // Clean up resources
    //    _executionContext.GraphicsDevice.WaitForIdle();

    //    _executionContext.Renderer.Dispose();
    //    cl.Dispose();

    //    _executionContext.GraphicsDevice.Dispose();

    //    FontFactory.Dispose();
    //}

    public void Exit()
    {
        if (Instance == null)
            throw new InvalidOperationException("There is no application running.");

        SDL.DestroyWindow(Window);
    }

    public void SetSize(Vector2 size)
    {
        if (Instance == null)
            throw new InvalidOperationException("There is no application running.");

        SDL.SetWindowSize(Window, (int)size.X, (int)size.Y);

        Instance.MainForm.Size = size;
    }

    private void UpdateApplicationEvents()
    {
        //_dragDropEvents = [];
        _frameHandledDragDrops = [];
    }

    #region Window events

    private void Window_Shown()
    {
        _executionContext.MainForm.OnLoad();
    }

    private bool ShouldCancelClose()
    {
        // If any close blocking modal is open, cancel closing
        if (MainForm.HasBlockingModals())
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

    private async void IsClosing()
    {
        var args = new ClosingEventArgs();
        await MainForm.OnClosing(args);

        _isClosing = false;
        _shouldClose = !args.Cancel;
    }

    //private void Window_Resized()
    //{
    //    _executionContext.GraphicsDevice.MainSwapchain.Resize((uint)_executionContext.Window.Width, (uint)_executionContext.Window.Height);
    //    _executionContext.Renderer.WindowResized(_executionContext.Window.Width, _executionContext.Window.Height);

    //    _executionContext.MainForm.Size = new Vector2(_executionContext.Window.Width, _executionContext.Window.Height);

    //    _executionContext.MainForm.OnResized();
    //}

    //private void Window_DragDrop(DragDropEvent obj)
    //{
    //    Array.Resize(ref _frameHandledDragDrops, _frameHandledDragDrops.Length + 1);
    //    _frameHandledDragDrops[^1] = false;

    //    Array.Resize(ref _dragDropEvents, _dragDropEvents.Length + 1);
    //    _dragDropEvents[^1] = new DragDropEventEx(obj, Hexa.NET.ImGui.ImGui.GetMousePos());
    //}

    #endregion

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        UnhandledException?.Invoke(this, e.ExceptionObject as Exception);
    }

    //internal bool TryGetDragDrop(Rectangle controlRect, out DragDropEvent[] events)
    //{
    //    events = new DragDropEvent[_dragDropEvents.Length];
    //    var index = 0;

    //    for (var i = 0; i < _frameHandledDragDrops.Length; i++)
    //    {
    //        if (_frameHandledDragDrops[i] || _dragDropEvents[i].IsEmpty)
    //            continue;

    //        if (!controlRect.Contains(new Point((int)_dragDropEvents[i].MousePosition.X, (int)_dragDropEvents[i].MousePosition.Y)))
    //            continue;

    //        events[index++] = _dragDropEvents[i].Event;
    //        _frameHandledDragDrops[i] = true;
    //    }

    //    Array.Resize(ref events, index);
    //    return events.Length > 0;
    //}
}

record ExecutionContext(Form MainForm, SDLWindowPtr Window, ImageFactory Images, IdFactory Ids);

//readonly struct DragDropEventEx
//{
//    public DragDropEvent Event { get; }
//    public Vector2 MousePosition { get; }

//    public bool IsEmpty => MousePosition == default && Event.File == null;

//    public DragDropEventEx(DragDropEvent evt, Vector2 mousePos)
//    {
//        Event = evt;
//        MousePosition = mousePos;
//    }
//}