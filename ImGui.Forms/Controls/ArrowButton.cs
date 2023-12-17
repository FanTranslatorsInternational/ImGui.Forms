using System;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class ArrowButton : Component
    {
        private const int ButtonSizeX_ = 11;
        private const int ButtonSizeY_ = 13;

        public KeyCommand KeyAction { get; set; }

        public ImGuiDir Direction { get; set; } = ImGuiDir.None;

        #region Events

        public event EventHandler Clicked;

        #endregion

        public override Size GetSize()
        {
            var padding = ImGuiNET.ImGui.GetStyle().FramePadding;

            return new Size((int)Math.Ceiling(ButtonSizeX_ + padding.X * 2), (int)Math.Ceiling(ButtonSizeY_ + padding.Y * 2));
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var enabled = Enabled;

            if (!enabled)
            {
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            }

            if ((ImGuiNET.ImGui.ArrowButton($"##{Id}", Direction) || IsKeyDown(KeyAction)) && Enabled)
                OnClicked();

            if (!enabled)
                ImGuiNET.ImGui.PopStyleColor(3);
        }

        private void OnClicked()
        {
            Clicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
