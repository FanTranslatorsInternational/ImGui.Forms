using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Extensions;
using ImGui.Forms.Factories;
using ImGui.Forms.Localization;
using ImGui.Forms.Modals;
using ImGui.Forms.Resources;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid.Sdl2;

namespace ImGui.Forms
{
    // HINT: Does not derive from Container to not be a component and therefore nestable into other containers
    public abstract class Form
    {
        private readonly IList<Modal> _modals = new List<Modal>();

        private Image<Rgba32> _icon;
        private bool _setIcon;

        #region Properties

        public int Id => IdFactory.Get(this);

        public LocalizedString Title { get; set; } = string.Empty;
        public Vector2 Size { get; set; } = new(700, 400);
        public int Width => (int)Size.X;
        public int Height => (int)Size.Y;

        public bool AllowDragDrop { get; set; }

        /// <summary>
        /// Gets and sets the applications icon.
        /// </summary>
        /// <remarks>The icon dimensions need to be a power of 2 (eg. 32, 64, 128, etc)</remarks>
        public Image<Rgba32> Icon
        {
            get => _icon;
            protected set
            {
                _icon = value;
                _setIcon = true;
            }
        }

        public MainMenuBar MenuBar { get; protected set; }

        public Component Content { get; protected set; }

        public Vector2 Padding
        {
            get => Style.GetStyleVector2(ImGuiStyleVar.WindowPadding);
            set => Style.SetStyle(ImGuiStyleVar.WindowPadding, value);
        }

        public FontResource DefaultFont { get; set; } = FontFactory.GetDefault(13);

        #endregion

        #region Events

        public event EventHandler<DragDropEvent[]> DragDrop;
        public event EventHandler Load;
        public event EventHandler Resized;
        public event Func<object, ClosingEventArgs, Task> Closing;

        #endregion

        internal void PushModal(Modal modal)
        {
            if (_modals.Count > 0)
                _modals.Last().ChildModal = modal;

            _modals.Add(modal);
        }

        internal void PopModal()
        {
            if (_modals.Count <= 0)
                return;

            _modals.Remove(_modals.Last());

            if (_modals.Count > 0)
                _modals.Last().ChildModal = null;
        }

        public bool HasOpenModals()
        {
            return _modals.Count > 0;
        }

        internal void Update()
        {
            // Set icon
            if (_setIcon)
            {
                Sdl2NativeExtensions.SetWindowIcon(Application.Instance.Window.SdlWindowHandle, Icon);
                _setIcon = false;
            }

            // Set window title
            Application.Instance.Window.Title = Title;

            // Begin window
            ImGuiNET.ImGui.Begin($"{Id}", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove);

            ImGuiNET.ImGui.SetWindowSize(Size, ImGuiCond.Always);

            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);
            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

            // Apply style
            Style.ApplyStyle();

            // Push font to default to
            ImFontPtr? fontPtr = DefaultFont?.GetPointer();
            if (fontPtr != null)
                ImGuiNET.ImGui.PushFont(fontPtr.Value);

            // Add menu bar
            MenuBar?.Update();
            var menuHeight = MenuBar?.Height ?? 0;

            // Add form controls
            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Padding);
            ImGuiNET.ImGui.SetWindowPos(new Vector2(0, menuHeight));

            var contentPos = ImGuiNET.ImGui.GetCursorScreenPos();
            var contentWidth = Content?.GetWidth(Width - (int)Padding.X * 2, Height - (int)Padding.Y * 2 - menuHeight) ?? 0;
            var contentHeight = Content?.GetHeight(Width - (int)Padding.X * 2, Height - (int)Padding.Y * 2 - menuHeight) ?? 0;
            var contentRect = new Veldrid.Rectangle((int)contentPos.X, (int)contentPos.Y, contentWidth, contentHeight);

            Content?.Update(contentRect);

            // Add modal
            var modal = _modals.Count > 0 ? _modals.First() : null;
            Modal.DrawModal(modal);

            // Handle Drag and Drop after rendering
            if (AllowDragDrop)
                if (Application.Instance.TryGetDragDrop(new Veldrid.Rectangle(0, 0, (int)Size.X, (int)Size.Y), out DragDropEvent[] dragDrops))
                    OnDragDrop(dragDrops);

            // End window
            ImGuiNET.ImGui.End();
        }

        protected void Close()
        {
            Application.Instance?.Window.Close();
        }

        internal bool HasBlockingModals()
        {
            return _modals.Any(x => x.BlockFormClosing);
        }

        #region Event Invokers

        internal void OnResized()
        {
            Resized?.Invoke(this, EventArgs.Empty);
        }

        internal void OnLoad()
        {
            Load?.Invoke(this, EventArgs.Empty);
        }

        internal async Task OnClosing(ClosingEventArgs e)
        {
            if (Closing == null)
                return;

            await Closing?.Invoke(this, e);
        }

        private void OnDragDrop(DragDropEvent[] e)
        {
            DragDrop?.Invoke(this, e);
        }

        #endregion
    }

    public class ClosingEventArgs : EventArgs
    {
        public bool Cancel { get; set; }
    }
}
