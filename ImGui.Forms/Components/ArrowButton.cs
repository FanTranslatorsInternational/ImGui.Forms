using System;
using ImGui.Forms.Components.Base;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Components
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
            return new Size(19, 19);
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

            if(!enabled)
                ImGuiNET.ImGui.PopStyleColor(3);
        }

        private void OnClicked()
        {
            Clicked?.Invoke(this, new EventArgs());
        }
    }
}
