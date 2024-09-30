using System;
using System.Drawing;
using System.Numerics;
using ImGui.Forms.Factories;
using ImGui.Forms.Localization;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Support.Veldrid.ImGui;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace ImGui.Forms
{
    public class Application
    {
        private bool _isClosing;
        private bool _shouldClose;

        private GraphicsBackend? _backend;

        private ExecutionContext _executionContext;

        private DragDropEventEx[] _dragDropEvents;
        private bool[] _frameHandledDragDrops;

        private KeyCommand _keyUpCommand;
        private KeyCommand _keyDownCommand;

        #region Static properties

        public static Application Instance { get; private set; }

        #endregion

        #region Properties

        public Form MainForm => _executionContext.MainForm;

        internal Sdl2Window Window => _executionContext.Window;

        public ILocalizer Localizer { get; }

        internal ImageFactory ImageFactory { get; private set; }

        #endregion

        #region Events

        public event EventHandler<Exception> UnhandledException;

        #endregion

        public Application(ILocalizer localizer = null, GraphicsBackend? backend = null)
        {
            _backend = backend;

            Localizer = localizer;

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        public void Execute(Form form)
        {
            if (Instance != null)
                throw new InvalidOperationException("There already is an application running.");

            CreateApplication(form);

            _executionContext.Window.Resized += Window_Resized;
            _executionContext.Window.DragDrop += Window_DragDrop;
            _executionContext.Window.KeyDown += Window_KeyDown;
            _executionContext.Window.KeyUp += Window_KeyUp;
            _executionContext.Window.Shown += Window_Shown;
            _executionContext.Window.SetCloseRequestedHandler(ShouldCancelClose);

            var cl = _executionContext.GraphicsDevice.ResourceFactory.CreateCommandList();

            // Main application loop
            while (_executionContext.Window.Exists)
            {
                if (!UpdateFrame(cl))
                    break;
            }

            // Clean up resources
            _executionContext.GraphicsDevice.WaitForIdle();

            _executionContext.Renderer.Dispose();
            cl.Dispose();

            _executionContext.GraphicsDevice.Dispose();

            FontFactory.Dispose();
        }

        public void Exit()
        {
            if (Instance == null)
                throw new InvalidOperationException("There is no application running.");

            Instance.Window.Close();
        }

        public void SetSize(Vector2 size)
        {
            if (Instance == null)
                throw new InvalidOperationException("There is no application running.");

            Instance.Window.Width = (int)size.X;
            Instance.Window.Height = (int)size.Y;

            Instance.MainForm.Size = size;
        }

        private void CreateApplication(Form form)
        {
            // Create window
            CreateWindow(form, out var window, out var gd);

            _executionContext = new ExecutionContext(form, gd, window);
            
            ImageFactory = new ImageFactory(gd, _executionContext.Renderer);

            FontFactory.Initialize(ImGuiNET.ImGui.GetIO(), _executionContext.Renderer);

            Instance = this;
        }

        private void CreateWindow(Form form, out Sdl2Window window, out GraphicsDevice gd)
        {
            var windowInfo = new WindowCreateInfo(50, 70, form.Width, form.Height, WindowState.Normal, form.Title);
            var graphicsDeviceOptions = new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true);

            if (_backend.HasValue)
            {
                if (TryCreateWindow(windowInfo, graphicsDeviceOptions, _backend.Value, out window, out gd))
                    return;

                throw new InvalidOperationException($"[ERROR] Can't create window with fixed backend {_backend}. Shutting down.");
            }

            GraphicsBackend defaultBackend = VeldridStartup.GetPlatformDefaultBackend();
            if (TryCreateWindow(windowInfo, graphicsDeviceOptions, defaultBackend, out window, out gd))
                return;

            for (var i = 0; i < 5; i++)
            {
                if (i == (int)defaultBackend)
                    continue;

                if (TryCreateWindow(windowInfo, graphicsDeviceOptions, (GraphicsBackend)i, out window, out gd))
                    return;
            }

            throw new InvalidOperationException("[ERROR] Can't create window with any backend. Shutting down.");
        }

        private bool TryCreateWindow(WindowCreateInfo windowInfo, GraphicsDeviceOptions graphicsDeviceOptions, GraphicsBackend backend, out Sdl2Window window, out GraphicsDevice gd)
        {
            window = null;
            gd = null;

            try
            {
                VeldridStartup.CreateWindowAndGraphicsDevice(windowInfo, graphicsDeviceOptions, backend,
                    out window, out gd);
                Console.WriteLine($"[INFO] Created window with backend {backend}.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Can't create window with backend {backend}: {e.Message}");
                return false;
            }

            return true;
        }

        private bool UpdateFrame(CommandList cl)
        {
            UpdateApplicationEvents();

            ImageFactory.FreeTextures();
            IdFactory.FreeIds();

            //FontFactory.InitializeFonts(ImGuiNET.ImGui.GetIO(), _executionContext.Renderer);

            // Snapshot current machine state
            var snapshot = _executionContext.Window.PumpEvents();

            if (_shouldClose)
                _executionContext.Window.Close();

            if (!_executionContext.Window.Exists)
                return false;

            _executionContext.Renderer.Update(1f / 60f, snapshot);

            // Update main form
            _executionContext.MainForm.Update();

            // Update frame buffer
            cl.Begin();
            cl.SetFramebuffer(_executionContext.GraphicsDevice.MainSwapchain.Framebuffer);
            cl.ClearColorTarget(0, new RgbaFloat(SystemColors.Control.R, SystemColors.Control.G, SystemColors.Control.B, 1f));
            _executionContext.Renderer.Render(_executionContext.GraphicsDevice, cl);
            cl.End();

            _executionContext.GraphicsDevice.SubmitCommands(cl);
            _executionContext.GraphicsDevice.SwapBuffers();

            return true;
        }

        private void UpdateApplicationEvents()
        {
            _dragDropEvents = Array.Empty<DragDropEventEx>();
            _keyUpCommand = default;
            _keyDownCommand = default;
            _frameHandledDragDrops = Array.Empty<bool>();
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

        private void Window_Resized()
        {
            _executionContext.GraphicsDevice.MainSwapchain.Resize((uint)_executionContext.Window.Width, (uint)_executionContext.Window.Height);
            _executionContext.Renderer.WindowResized(_executionContext.Window.Width, _executionContext.Window.Height);

            _executionContext.MainForm.Size = new Vector2(_executionContext.Window.Width, _executionContext.Window.Height);

            _executionContext.MainForm.OnResized();
        }

        private void Window_DragDrop(DragDropEvent obj)
        {
            Array.Resize(ref _frameHandledDragDrops, _frameHandledDragDrops.Length + 1);
            _frameHandledDragDrops[^1] = false;

            Array.Resize(ref _dragDropEvents, _dragDropEvents.Length + 1);
            _dragDropEvents[^1] = new DragDropEventEx(obj, ImGuiNET.ImGui.GetMousePos());
        }

        private void Window_KeyUp(KeyEvent obj)
        {
            _keyUpCommand = new KeyCommand(obj.Modifiers, obj.Key);
        }

        private void Window_KeyDown(KeyEvent obj)
        {
            _keyDownCommand = new KeyCommand(obj.Modifiers, obj.Key);
        }

        #endregion

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException?.Invoke(this, e.ExceptionObject as Exception);
        }

        internal bool TryGetKeyDownCommand(out KeyCommand keyDown)
        {
            keyDown = _keyDownCommand;

            return !_keyDownCommand.IsEmpty;
        }

        internal bool TryGetKeyUpCommand(out KeyCommand keyUp)
        {
            keyUp = _keyUpCommand;

            return !_keyUpCommand.IsEmpty;
        }

        internal bool TryGetDragDrop(Veldrid.Rectangle controlRect, out DragDropEvent[] events)
        {
            events = new DragDropEvent[_dragDropEvents.Length];
            var index = 0;

            for (var i = 0; i < _frameHandledDragDrops.Length; i++)
            {
                if (_frameHandledDragDrops[i] || _dragDropEvents[i].IsEmpty)
                    continue;

                if (!controlRect.Contains(new Veldrid.Point((int)_dragDropEvents[i].MousePosition.X, (int)_dragDropEvents[i].MousePosition.Y)))
                    continue;

                events[index++] = _dragDropEvents[i].Event;
                _frameHandledDragDrops[i] = true;
            }

            Array.Resize(ref events, index);
            return events.Length > 0;
        }
    }

    class ExecutionContext
    {
        public Form MainForm { get; }

        public GraphicsDevice GraphicsDevice { get; }

        public Sdl2Window Window { get; }

        public ImGuiRenderer Renderer { get; }

        public ExecutionContext(Form mainForm, GraphicsDevice gd, Sdl2Window window)
        {
            MainForm = mainForm;
            GraphicsDevice = gd;
            Window = window;

            Renderer = new ImGuiRenderer(gd, gd.MainSwapchain.Framebuffer.OutputDescription, mainForm.Width, mainForm.Height);
        }
    }

    readonly struct DragDropEventEx
    {
        public DragDropEvent Event { get; }
        public Vector2 MousePosition { get; }

        public bool IsEmpty => MousePosition == default && Event.File == null;

        public DragDropEventEx(DragDropEvent evt, Vector2 mousePos)
        {
            Event = evt;
            MousePosition = mousePos;
        }
    }
}
