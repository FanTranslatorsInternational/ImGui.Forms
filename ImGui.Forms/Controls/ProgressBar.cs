using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class ProgressBar : Component
    {
        public int Minimum { get; set; }

        public int Maximum { get; set; } = 100;

        public int Value { get; set; }

        public string Text { get; set; }

        public Size Size { get; set; } = Size.Parent;

        public FontResource Font { get; set; }

        public override Size GetSize()
        {
            return Size;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            // Draw progress bar
            var barWidth = (float)Math.Ceiling((float)contentRect.Width / Maximum * Value);
            ImGuiNET.ImGui.GetWindowDrawList().AddRectFilled(new Vector2(contentRect.X, contentRect.Y), new Vector2(contentRect.X + barWidth, contentRect.Y + contentRect.Height), 0xFF27BB65);

            // Draw border
            ImGuiNET.ImGui.GetWindowDrawList().AddRect(new Vector2(contentRect.X, contentRect.Y), new Vector2(contentRect.X + contentRect.Width, contentRect.Y + contentRect.Height), 0xFF828282);

            // Draw text
            var text = Text ?? string.Empty;
            var textSize = ImGuiNET.ImGui.CalcTextSize(text);
            var textPos = new Vector2(contentRect.X + (contentRect.Width - textSize.X) / 2, contentRect.Y + (contentRect.Height - textSize.Y) / 2);

            if (Font == null)
                ImGuiNET.ImGui.GetWindowDrawList().AddText(textPos, 0xFFFFFFFF, text);
            else
                ImGuiNET.ImGui.GetWindowDrawList().AddText((ImFontPtr)Font, Font.Size, textPos, 0xFFFFFFFF, text);
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
