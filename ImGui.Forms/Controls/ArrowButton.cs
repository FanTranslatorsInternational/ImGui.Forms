using System;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class ArrowButton : Component
    {
        public ImGuiDir Direction { get; set; } = ImGuiDir.None;

        #region Events

        public event EventHandler Clicked;

        #endregion

        public override Size GetSize()
        {
            var height = FontResource.GetCurrentLineHeight();
            var padding = ImGuiNET.ImGui.GetStyle().FramePadding;

            return new Size((int)Math.Ceiling(height + padding.X*2), (int)Math.Ceiling(height + padding.Y*2));
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var enabled = Enabled;

            if (!enabled)
            {
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, 0xFF666666);
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF666666);
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF666666);
            }

            if (ImGuiNET.ImGui.ArrowButton($"##{Id}", Direction) && Enabled)
                OnClicked();

            if (!enabled)
                ImGuiNET.ImGui.PopStyleColor(3);
        }

        private void OnClicked()
        {
            Clicked?.Invoke(this, new EventArgs());
        }
    }
}
