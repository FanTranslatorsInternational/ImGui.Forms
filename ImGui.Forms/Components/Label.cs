using System;
using ImGui.Forms.Components.Base;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Components
{
    public class Label : Component
    {
        public string Caption { get; set; } = string.Empty;

        public FontResource Font { get; set; }

        public override Size GetSize()
        {
            ApplyStyles();

            var textSize = ImGuiNET.ImGui.CalcTextSize(Caption ?? string.Empty);
            var width = (int)Math.Ceiling(textSize.X);
            var height = (int)Math.Ceiling(textSize.Y);

            RemoveStyles();

            return new Size(width, height);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            // TODO: Add property to decide if label should get assigned componentWidth
            ImGuiNET.ImGui.Text(Caption);
        }

        protected override void ApplyStyles()
        {
            if (Font != null)
                ImGuiNET.ImGui.PushFont((ImFontPtr)Font);
        }

        protected override void RemoveStyles()
        {
            if (Font != null)
                ImGuiNET.ImGui.PopFont();
        }
    }
}
