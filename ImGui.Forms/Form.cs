using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Components.Base;
using ImGui.Forms.Components.Menu;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using ImGuiNET;
using Rectangle = Veldrid.Rectangle;

namespace ImGui.Forms
{
    // HINT: Does not derive from Container to not be a component and therefore nestable into other containers
    public class Form
    {
        private readonly IList<Modal> _modals = new List<Modal>();

        public string Title { get; set; } = string.Empty;
        public int Width { get; set; } = 700;
        public int Height { get; set; } = 400;

        public MainMenuBar MainMenuBar { get; set; }

        public Component Content { get; set; }

        public Vector2 Padding { get; set; } = new Vector2(2, 2);

        public FontResource DefaultFont { get; set; }

        #region Events

        public event EventHandler Resized;

        #endregion

        public void PushModal(Modal modal)
        {
            if (_modals.Count > 0)
                _modals.Last().ChildModal = modal;

            _modals.Add(modal);
        }

        public void PopModal()
        {
            _modals.Remove(_modals.Last());

            if (_modals.Count > 0)
                _modals.Last().ChildModal = null;
        }

        public void Update()
        {
            // Begin window
            ImGuiNET.ImGui.Begin(Title, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove);

            ImGuiNET.ImGui.SetWindowSize(new Vector2(Width, Height), ImGuiCond.Always);

            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);
            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

            // Push font to default to
            if (DefaultFont != null)
                ImGuiNET.ImGui.PushFont((ImFontPtr)DefaultFont);

            // Add menu bar
            MainMenuBar?.Update();
            var menuHeight = MainMenuBar?.Height ?? 0;

            // Add form controls
            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Padding);
            ImGuiNET.ImGui.SetWindowPos(new Vector2(0, menuHeight));

            var contentPos = ImGuiNET.ImGui.GetCursorScreenPos();
            var contentWidth = Content?.GetWidth(Width - (int)Padding.X * 2) ?? 0;
            var contentHeight = Content?.GetHeight(Height - (int)Padding.Y * 2 - menuHeight) ?? 0;
            Content?.Update(new Rectangle((int)contentPos.X, (int)contentPos.Y, contentWidth, contentHeight));

            // Add modals
            var modal = _modals.Count > 0 ? _modals.First() : null;
            if (modal != null)
            {
                var modalPos = new Vector2((Width - modal.Width) / 2f - Padding.X, (Height - modal.Height) / 2f - contentPos.Y / 2f);
                var modalContentSize = new Vector2(modal.Width, modal.Height);
                var modalSize = modalContentSize + new Vector2(Padding.X * 2, Modal.HeaderHeight + Padding.Y * 2);

                ImGuiNET.ImGui.SetNextWindowPos(modalPos);
                ImGuiNET.ImGui.SetNextWindowSize(modalSize);
                modal.Update(new Rectangle((int)modalPos.X, (int)modalPos.Y, (int)modalContentSize.X, (int)modalContentSize.Y));
            }

            // End window
            ImGuiNET.ImGui.End();
        }

        internal void OnResized()
        {
            Resized?.Invoke(this, new EventArgs());
        }
    }
}
