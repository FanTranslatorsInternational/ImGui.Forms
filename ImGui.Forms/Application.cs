using System;
using System.Drawing;
using System.Numerics;
using ImGui.Forms.Factories;
using ImGui.Forms.Localization;
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

        private ExecutionContext _executionContext;

        private DragDropEventEx _dragDropEvent;
        private bool _frameHandledDragDrop;

        #region Static properties

        public static Application Instance { get; private set; }

        public static FontFactory FontFactory { get; } = new FontFactory();

        #endregion

        #region Properties

        #region Factories

        public IdFactory IdFactory { get; private set; }

        internal ImageFactory ImageFactory { get; private set; }

        #endregion

        public Form MainForm => _executionContext.MainForm;

        internal Sdl2Window Window => _executionContext.Window;

        public ILocalizer Localizer { get; }

        #endregion

        #region Events

        public event EventHandler<Exception> UnhandledException;

        #endregion

        public Application(ILocalizer localizer = null)
        {
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

        private void CreateApplication(Form form)
        {
            // Create window
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(20, 20, form.Width, form.Height, WindowState.Normal, form.Title),
                new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
                out var window,
                out var gd);

            _executionContext = new ExecutionContext(form, gd, window);

            IdFactory = new IdFactory();
            ImageFactory = new ImageFactory(gd, _executionContext.Renderer);

            FontFactory.Initialize(ImGuiNET.ImGui.GetIO(), _executionContext.Renderer);

            Instance = this;
        }

        private bool UpdateFrame(CommandList cl)
        {
            _dragDropEvent = default;
            _frameHandledDragDrop = false;

            ImageFactory.FreeTextures();

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
            _executionContext.GraphicsDevice.SwapBuffers(_executionContext.GraphicsDevice.MainSwapchain);

            return true;
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
            _dragDropEvent = new DragDropEventEx(obj, ImGuiNET.ImGui.GetMousePos());
        }

        #endregion

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException?.Invoke(this, e.ExceptionObject as Exception);
        }

        internal bool TryGetDragDrop(Veldrid.Rectangle controlRect, out DragDropEventEx obj)
        {
            obj = _dragDropEvent;

            // Try get drag drop event
            if (_frameHandledDragDrop || _dragDropEvent.MousePosition == default)
                return false;

            // Check if control contains dropped element
            return _frameHandledDragDrop = controlRect.Contains(new Veldrid.Point((int)obj.MousePosition.X, (int)obj.MousePosition.Y));
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

    struct DragDropEventEx
    {
        public DragDropEvent Event { get; }
        public Vector2 MousePosition { get; }

        public DragDropEventEx(DragDropEvent evt, Vector2 mousePos)
        {
            Event = evt;
            MousePosition = mousePos;
        }
    }
}
