using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.SDL3;
using Hexa.NET.SDL3;
using ImGui.Forms.Extensions;
using ImGui.Forms.Factories;
using ImGui.Forms.Localization;
using ImGui.Forms.Support;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImSDLEvent = Hexa.NET.ImGui.Backends.SDL3.SDLEvent;
using ImSDLRenderer = Hexa.NET.ImGui.Backends.SDL3.SDLRenderer;
using ImSDLWindow = Hexa.NET.ImGui.Backends.SDL3.SDLWindow;
using Rectangle = ImGui.Forms.Support.Rectangle;
using SDLEvent = Hexa.NET.SDL3.SDLEvent;
using SDLRenderer = Hexa.NET.SDL3.SDLRenderer;
using SDLWindow = Hexa.NET.SDL3.SDLWindow;
using SDLWindowPtr = Hexa.NET.SDL3.SDLWindowPtr;

namespace ImGui.Forms;

public class Application
{
    private bool _isClosing;
    private bool _shouldClose;

    private ExecutionContext? _executionContext;
    private readonly List<RenderPrepareAction> _renderPrepareActions = [];
    private readonly List<RenderAction> _renderActions = [];
    private readonly List<DragDropEvent> _dragDropEvents = [];
    private readonly List<bool> _frameHandledDragDrops = [];

#pragma warning disable CS8618
    public static Application Instance { get; private set; }
#pragma warning restore CS8618

    public Form? MainForm => _executionContext?.MainForm;
    internal SDLWindowPtr? Window => _executionContext?.Window;
    internal unsafe SDLRenderer* Renderer => _executionContext == null ? (SDLRenderer*)0 : _executionContext.Renderer;
    internal ImageFactory? Images => _executionContext?.Images;
    internal IdFactory? Ids => _executionContext?.Ids;

    public ILocalizer? Localizer { get; private set; }
    public event EventHandler<Exception?>? UnhandledException;

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

        SDLRenderer* renderer = SDL.CreateRenderer(window, (byte*)null);
        if (renderer == null)
        {
            Console.WriteLine($"Error: SDL_CreateRenderer(): {SDL.GetErrorS()}");
            return;
        }

        SDL.SetRenderVSync(renderer, 1);

        var ctx = Hexa.NET.ImGui.ImGui.CreateContext();
        Hexa.NET.ImGui.ImGui.SetCurrentContext(ctx);

        ImGuiIOPtr io = Hexa.NET.ImGui.ImGui.GetIO();
        io.IniFilename = null;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.NavEnableGamepad;
        io.ConfigErrorRecoveryEnableAssert = false;
        io.ConfigErrorRecovery = false;
        io.ConfigErrorRecoveryEnableDebugLog = false;
        io.ConfigErrorRecoveryEnableTooltip = false;
        io.ConfigDpiScaleFonts = true;
        io.ConfigDpiScaleViewports = true;

        ImGuiPlatformIOPtr platformIo = Hexa.NET.ImGui.ImGui.GetPlatformIO();
        platformIo.PlatformGetClipboardTextFn = (void*)Marshal.GetFunctionPointerForDelegate(Sdl2NativeExtensions.GetClipboardText);
        platformIo.PlatformSetClipboardTextFn = (void*)Marshal.GetFunctionPointerForDelegate(Sdl2NativeExtensions.SetClipboardText);

        _executionContext = new ExecutionContext(form, window, renderer, new ImageFactory(renderer), new IdFactory());
        FontFactory.Initialize(io);

        ImGuiImplSDL3.SetCurrentContext(ctx);
        ImGuiImplSDL3.InitForSDLRenderer((ImSDLWindow*)window, (ImSDLRenderer*)renderer);
        ImGuiImplSDL3.SDLRenderer3Init((ImSDLRenderer*)renderer);

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

            ImGuiImplSDL3.SDLRenderer3NewFrame();
            ImGuiImplSDL3.NewFrame();
            Hexa.NET.ImGui.ImGui.NewFrame();

            if (shouldInvokeLoad)
            {
                form.OnLoad();
                shouldInvokeLoad = false;
            }

            form.Update();
            Hexa.NET.ImGui.ImGui.Render();
            ImDrawData* drawData = Hexa.NET.ImGui.ImGui.GetDrawData();
            bool isMinimized = drawData->DisplaySize.X <= 0 || drawData->DisplaySize.Y <= 0;

            if (!isMinimized)
            {
                ExecuteQueuedRenderPrepares(renderer);
                SDL.SetRenderDrawColorFloat(renderer, .45f, .55f, .60f, 1f);
                SDL.RenderClear(renderer);
                ImGuiImplSDL3.SDLRenderer3RenderDrawData(drawData, (ImSDLRenderer*)renderer);
                ExecuteQueuedRenders(renderer);
                SDL.RenderPresent(renderer);
            }
        }

        ImGuiImplSDL3.Shutdown();
        ImGuiImplSDL3.SDLRenderer3Shutdown();
        Hexa.NET.ImGui.ImGui.DestroyContext();
        FontFactory.Dispose();
        _executionContext.Images.Dispose();
        SDL.StopTextInput(window);
        SDL.DestroyRenderer(renderer);
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
        _dragDropEvents.Clear();
        _frameHandledDragDrops.Clear();
        _renderPrepareActions.Clear();
        _renderActions.Clear();
    }

    internal void EnqueueRenderPrepareAction(RenderPrepareAction action) => _renderPrepareActions.Add(action);
    internal void EnqueueRenderAction(RenderAction action) => _renderActions.Add(action);

    private unsafe void ExecuteQueuedRenderPrepares(SDLRenderer* renderer)
    {
        foreach (var action in _renderPrepareActions)
            action(renderer);
    }

    private unsafe void ExecuteQueuedRenders(SDLRenderer* renderer)
    {
        foreach (var action in _renderActions)
            action(renderer);
    }

    private bool ShouldCancelClose()
    {
        if (_executionContext == null)
            return false;
        if (_executionContext.MainForm.HasBlockingModals())
            return true;

        if (!_isClosing && !_shouldClose)
        {
            _isClosing = true;
            IsClosing();
        }

        return _isClosing || !_shouldClose;
    }

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

        _frameHandledDragDrops.Add(false);
        _dragDropEvents.Add(new DragDropEvent(path, Hexa.NET.ImGui.ImGui.GetMousePos()));
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        UnhandledException?.Invoke(this, e.ExceptionObject as Exception);
    }

    internal bool TryGetDragDrop(Rectangle controlRect, out string[] files)
    {
        files = new string[_dragDropEvents.Count];
        var index = 0;

        for (var i = 0; i < _frameHandledDragDrops.Count; i++)
        {
            if (_frameHandledDragDrops[i] || _dragDropEvents[i].IsEmpty)
                continue;
            if (!controlRect.Contains(_dragDropEvents[i].MousePosition))
                continue;

            files[index++] = _dragDropEvents[i].File;
            _frameHandledDragDrops[i] = true;
        }

        Array.Resize(ref files, index);
        return files.Length > 0;
    }
}

internal unsafe delegate void RenderPrepareAction(SDLRenderer* renderer);
internal unsafe delegate void RenderAction(SDLRenderer* renderer);
internal delegate void ImGuiRenderAction();

internal unsafe sealed class ExecutionContext(Form mainForm, SDLWindowPtr window, SDLRenderer* renderer, ImageFactory images, IdFactory ids)
{
    public Form MainForm { get; } = mainForm;
    public SDLWindowPtr Window { get; } = window;
    public SDLRenderer* Renderer { get; } = renderer;
    public ImageFactory Images { get; } = images;
    public IdFactory Ids { get; } = ids;
}

internal readonly struct DragDropEvent(string file, Vector2 mousePos)
{
    public string File { get; } = file;
    public Vector2 MousePosition { get; } = mousePos;
    public bool IsEmpty => MousePosition == default && File == null;
}
/*
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.SDL3;
using Hexa.NET.SDL3;
using ImGui.Forms.Extensions;
using ImGui.Forms.Factories;
using ImGui.Forms.Localization;
using ImGui.Forms.Support;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImSDLEvent = Hexa.NET.ImGui.Backends.SDL3.SDLEvent;
using ImSDLRenderer = Hexa.NET.ImGui.Backends.SDL3.SDLRenderer;
using ImSDLWindow = Hexa.NET.ImGui.Backends.SDL3.SDLWindow;
using Rectangle = ImGui.Forms.Support.Rectangle;
using SDLEvent = Hexa.NET.SDL3.SDLEvent;
using SDLRenderer = Hexa.NET.SDL3.SDLRenderer;
using SDLWindow = Hexa.NET.SDL3.SDLWindow;
using SDLWindowPtr = Hexa.NET.SDL3.SDLWindowPtr;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace ImGui.Forms;

public class Application
{
    private bool _isClosing;
    private bool _shouldClose;

    private ExecutionContext? _executionContext;
    private readonly List<RenderPrepareAction> _renderPrepareActions = [];
    private readonly List<RenderAction> _renderActions = [];

    private readonly List<DragDropEvent> _dragDropEvents = [];
    private readonly List<bool> _frameHandledDragDrops = [];

    #region Static properties

#pragma warning disable CS8618
    public static Application Instance { get; private set; }
#pragma warning restore CS8618

    #endregion

    #region Properties

    public Form? MainForm => _executionContext?.MainForm;

    internal SDLWindowPtr? Window => _executionContext?.Window;
    internal unsafe SDLRenderer* Renderer => _executionContext == null ? (SDLRenderer*)0 : _executionContext.Renderer;
    internal ImageFactory? Images => _executionContext?.Images;
    internal IdFactory? Ids => _executionContext?.Ids;

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

        SDLRenderer* renderer = SDL.CreateRenderer(window, (byte*)null);
        if (renderer == null)
        {
            Console.WriteLine($"Error: SDL_CreateRenderer(): {SDL.GetErrorS()}");
            return;
        }

        SDL.SetRenderVSync(renderer, 1);

        var ctx = Hexa.NET.ImGui.ImGui.CreateContext();
        Hexa.NET.ImGui.ImGui.SetCurrentContext(ctx);

        ImGuiIOPtr io = Hexa.NET.ImGui.ImGui.GetIO();
        io.IniFilename = null;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.NavEnableGamepad;
        io.ConfigErrorRecoveryEnableAssert = false;
        io.ConfigErrorRecovery = false;
        io.ConfigErrorRecoveryEnableDebugLog = false;
        io.ConfigErrorRecoveryEnableTooltip = false;
        io.ConfigDpiScaleFonts = true;
        io.ConfigDpiScaleViewports = true;

        ImGuiPlatformIOPtr platformIo = Hexa.NET.ImGui.ImGui.GetPlatformIO();
        platformIo.PlatformGetClipboardTextFn = (void*)Marshal.GetFunctionPointerForDelegate(Sdl2NativeExtensions.GetClipboardText);
        platformIo.PlatformSetClipboardTextFn = (void*)Marshal.GetFunctionPointerForDelegate(Sdl2NativeExtensions.SetClipboardText);

        _executionContext = new ExecutionContext(form, window, renderer, new ImageFactory(renderer), new IdFactory());
        FontFactory.Initialize(io);

        ImGuiImplSDL3.SetCurrentContext(ctx);
        ImGuiImplSDL3.InitForSDLRenderer((ImSDLWindow*)window, (ImSDLRenderer*)renderer);
        ImGuiImplSDL3.SDLRenderer3Init((ImSDLRenderer*)renderer);

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

            ImGuiImplSDL3.SDLRenderer3NewFrame();
            ImGuiImplSDL3.NewFrame();
            Hexa.NET.ImGui.ImGui.NewFrame();

            if (shouldInvokeLoad)
            {
                form.OnLoad();
                shouldInvokeLoad = false;
            }

            form.Update();
            Hexa.NET.ImGui.ImGui.Render();
            ImDrawData* drawData = Hexa.NET.ImGui.ImGui.GetDrawData();
            bool isMinimized = drawData->DisplaySize.X <= 0 || drawData->DisplaySize.Y <= 0;

            if (!isMinimized)
            {
                ExecuteQueuedRenderPrepares(renderer);
                SDL.SetRenderDrawColorFloat(renderer, .45f, .55f, .60f, 1f);
                SDL.RenderClear(renderer);
                ImGuiImplSDL3.SDLRenderer3RenderDrawData(drawData, (ImSDLRenderer*)renderer);
                ExecuteQueuedRenders(renderer);
                SDL.RenderPresent(renderer);
            }
        }

        ImGuiImplSDL3.Shutdown();
        ImGuiImplSDL3.SDLRenderer3Shutdown();
        Hexa.NET.ImGui.ImGui.DestroyContext();

        FontFactory.Dispose();
        _executionContext.Images.Dispose();

        SDL.StopTextInput(window);
        SDL.DestroyRenderer(renderer);
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
        _dragDropEvents.Clear();
        _frameHandledDragDrops.Clear();
        _renderPrepareActions.Clear();
        _renderActions.Clear();
    }

    internal void EnqueueRenderPrepareAction(RenderPrepareAction action)
    {
        _renderPrepareActions.Add(action);
    }

    internal void EnqueueRenderAction(RenderAction action)
    {
        _renderActions.Add(action);
    }

    private unsafe void ExecuteQueuedRenderPrepares(SDLRenderer* renderer)
    {
        foreach (var action in _renderPrepareActions)
            action(renderer);
    }

    private unsafe void ExecuteQueuedRenders(SDLRenderer* renderer)
    {
        foreach (var action in _renderActions)
            action(renderer);
    }

    private bool ShouldCancelClose()
    {
        if (_executionContext == null)
            return false;

        if (_executionContext.MainForm.HasBlockingModals())
            return true;

        if (!_isClosing && !_shouldClose)
        {
            _isClosing = true;
            IsClosing();
        }

        return _isClosing || !_shouldClose;
    }

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

        _frameHandledDragDrops.Add(false);
        _dragDropEvents.Add(new DragDropEvent(path, Hexa.NET.ImGui.ImGui.GetMousePos()));
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        UnhandledException?.Invoke(this, e.ExceptionObject as Exception);
    }

    internal bool TryGetDragDrop(Rectangle controlRect, out string[] files)
    {
        files = new string[_dragDropEvents.Count];
        var index = 0;

        for (var i = 0; i < _frameHandledDragDrops.Count; i++)
        {
            if (_frameHandledDragDrops[i] || _dragDropEvents[i].IsEmpty)
                continue;

            if (!controlRect.Contains(_dragDropEvents[i].MousePosition))
                continue;

            files[index++] = _dragDropEvents[i].File;
            _frameHandledDragDrops[i] = true;
        }

        Array.Resize(ref files, index);
        return files.Length > 0;
    }
}

internal unsafe delegate void RenderPrepareAction(SDLRenderer* renderer);
internal unsafe delegate void RenderAction(SDLRenderer* renderer);
internal delegate void ImGuiRenderAction();

internal unsafe sealed class ExecutionContext(Form mainForm, SDLWindowPtr window, SDLRenderer* renderer, ImageFactory images, IdFactory ids)
{
    public Form MainForm { get; } = mainForm;
    public SDLWindowPtr Window { get; } = window;
    public SDLRenderer* Renderer { get; } = renderer;
    public ImageFactory Images { get; } = images;
    public IdFactory Ids { get; } = ids;
}

internal readonly struct DragDropEvent(string file, Vector2 mousePos)
{
    public string File { get; } = file;
    public Vector2 MousePosition { get; } = mousePos;
    public bool IsEmpty => MousePosition == default && File == null;
}
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Backends.SDL3;
using Hexa.NET.SDL3;
using ImGui.Forms.Extensions;
using ImGui.Forms.Factories;
using ImGui.Forms.Localization;
using ImGui.Forms.Support;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImSDLEvent = Hexa.NET.ImGui.Backends.SDL3.SDLEvent;
using ImSDLWindow = Hexa.NET.ImGui.Backends.SDL3.SDLWindow;
using Rectangle = ImGui.Forms.Support.Rectangle;
using SDLEvent = Hexa.NET.SDL3.SDLEvent;
using SDLGLContext = Hexa.NET.SDL3.SDLGLContext;
using SDLWindow = Hexa.NET.SDL3.SDLWindow;
using SDLWindowPtr = Hexa.NET.SDL3.SDLWindowPtr;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace ImGui.Forms;

public class Application
{
    private bool _isClosing;
    private bool _shouldClose;

    private ExecutionContext? _executionContext;
    private readonly List<RenderPrepareAction> _renderPrepareActions = [];
    private readonly List<RenderAction> _renderActions = [];

    private readonly List<DragDropEvent> _dragDropEvents = [];
    private readonly List<bool> _frameHandledDragDrops = [];

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

        SDL.GLSetAttribute(SDLGLAttr.ContextMajorVersion, 3);
        SDL.GLSetAttribute(SDLGLAttr.ContextMinorVersion, 3);
        SDL.GLSetAttribute(SDLGLAttr.ContextProfileMask, 1);
        SDL.GLSetAttribute(SDLGLAttr.Doublebuffer, 1);
        SDL.GLSetAttribute(SDLGLAttr.DepthSize, 24);

        var windowFlags = SDLWindowFlags.Resizable | SDLWindowFlags.Hidden | SDLWindowFlags.HighPixelDensity;
        SDLWindow* window = SDL.CreateWindow(form.Title, form.Width, form.Height, (ulong)windowFlags);
        if (window == null)
        {
            Console.WriteLine($"Error: SDL_CreateWindow(): {SDL.GetErrorS()}");
            return;
        }

        SDL.SetWindowPosition(window, 50, 70);
        SDL.ShowWindow(window);

        SDLGLContext glContext = SDL.GLCreateContext(window);
        if (glContext.Handle == 0)
        {
            Console.WriteLine($"Error: SDL_GL_CreateContext(): {SDL.GetErrorS()}");
            return;
        }

        SDL.GLMakeCurrent(window, glContext);
        SDL.GLSetSwapInterval(1);

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

        _executionContext = new ExecutionContext(form, window, glContext, new ImageFactory(), new IdFactory());
        FontFactory.Initialize(io);

        ImGuiImplSDL3.SetCurrentContext(ctx);
        ImGuiImplSDL3.InitForOpenGL((ImSDLWindow*)window, (void*)glContext.Handle);
        ImGuiImplOpenGL3.Init("#version 330 core");

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

            ImGuiImplOpenGL3.NewFrame();
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

            if (!isMinimized)
            {
                int width = 0;
                int height = 0;
                SDL.GetWindowSize(window, ref width, ref height);

                GL.Viewport(0, 0, width, height);
                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(DepthFunction.Lequal);
                GL.ClearColor(.45f, .55f, .60f, 1f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                ExecuteQueuedRenderPrepares();
                ExecuteQueuedRenders();
                ImGuiImplOpenGL3.RenderDrawData(drawData);
                SDL.GLSwapWindow(window);
            }
        }

        ImGuiImplOpenGL3.Shutdown();
        ImGuiImplSDL3.Shutdown();
        Hexa.NET.ImGui.ImGui.DestroyContext();

        FontFactory.Dispose();
        _executionContext.Images.Dispose();

        SDL.StopTextInput(window);
        SDL.GLDestroyContext(glContext);
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
        _dragDropEvents.Clear();
        _frameHandledDragDrops.Clear();
        _renderPrepareActions.Clear();
        _renderActions.Clear();
    }

    internal void EnqueueRenderPrepareAction(RenderPrepareAction action)
    {
        _renderPrepareActions.Add(action);
    }

    internal void EnqueueRenderAction(RenderAction action)
    {
        _renderActions.Add(action);
    }

    private void ExecuteQueuedRenderPrepares()
    {
        foreach (var action in _renderPrepareActions)
            action();
    }

    private void ExecuteQueuedRenders()
    {
        foreach (var action in _renderActions)
            action();
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

        _frameHandledDragDrops.Add(false);
        _dragDropEvents.Add(new DragDropEvent(path, Hexa.NET.ImGui.ImGui.GetMousePos()));
    }

    #endregion

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        UnhandledException?.Invoke(this, e.ExceptionObject as Exception);
    }

    internal bool TryGetDragDrop(Rectangle controlRect, out string[] files)
    {
        files = new string[_dragDropEvents.Count];
        var index = 0;

        for (var i = 0; i < _frameHandledDragDrops.Count; i++)
        {
            if (_frameHandledDragDrops[i] || _dragDropEvents[i].IsEmpty)
                continue;

            if (!controlRect.Contains(_dragDropEvents[i].MousePosition))
                continue;

            files[index++] = _dragDropEvents[i].File;
            _frameHandledDragDrops[i] = true;
        }

        Array.Resize(ref files, index);
        return files.Length > 0;
    }
}

internal delegate void RenderPrepareAction();
internal delegate void RenderAction();
internal delegate void ImGuiRenderAction();

internal sealed class ExecutionContext(Form mainForm, SDLWindowPtr window, SDLGLContext glContext, ImageFactory images, IdFactory ids)
{
    public Form MainForm { get; } = mainForm;
    public SDLWindowPtr Window { get; } = window;
    public SDLGLContext GLContext { get; } = glContext;
    public ImageFactory Images { get; } = images;
    public IdFactory Ids { get; } = ids;
}

internal readonly struct DragDropEvent(string file, Vector2 mousePos)
{
    public string File { get; } = file;
    public Vector2 MousePosition { get; } = mousePos;

    public bool IsEmpty => MousePosition == default && File == null;
}
*/