using System;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class ArrowButton : Component
    {
        public ImGuiDir Direction { get; set; } = ImGuiDir.None;

        public bool Enabled { get; set; } = true;

        #region Events

        public event EventHandler Clicked;

        #endregion

        public override Size GetSize()
        {
            var size = ImGuiNET.ImGui.CalcTextSize("A").Y;
            var padding = ImGuiNET.ImGui.GetStyle().FramePadding;

            return new Size((int)Math.Ceiling(size + padding.X*2), (int)Math.Ceiling(size + padding.Y*2));
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
