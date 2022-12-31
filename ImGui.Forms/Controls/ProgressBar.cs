using System;
using System.Drawing;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Localization;
using ImGui.Forms.Resources;
using ImGuiNET;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls
{
    public class ProgressBar : Component
    {
        public int Minimum { get; set; }

        public int Maximum { get; set; } = 100;

        public int Value { get; set; }

        public LocalizedString Text { get; set; }

        public Size Size { get; set; } = Size.Parent;

        public FontResource Font { get; set; }

        public Color ProgressColor { get; set; } = Color.FromArgb(0x27, 0xBB, 0x65);

        public override Size GetSize()
        {
            return Size;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            // Draw progress bar
            var barWidth = (float)Math.Ceiling((float)contentRect.Width / Maximum * Value);
            ImGuiNET.ImGui.GetWindowDrawList().AddRectFilled(new Vector2(contentRect.X, contentRect.Y), new Vector2(contentRect.X + barWidth, contentRect.Y + contentRect.Height), ProgressColor.ToUInt32());

            // Draw border
            ImGuiNET.ImGui.GetWindowDrawList().AddRect(new Vector2(contentRect.X, contentRect.Y), new Vector2(contentRect.X + contentRect.Width, contentRect.Y + contentRect.Height), ImGuiNET.ImGui.GetColorU32(ImGuiCol.Border));

            // Draw text
            var textSize = FontResource.MeasureText(Text);
            var textPos = new Vector2(contentRect.X + (contentRect.Width - textSize.X) / 2, contentRect.Y + (contentRect.Height - textSize.Y) / 2);

            if (Font == null)
                ImGuiNET.ImGui.GetWindowDrawList().AddText(textPos, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text), Text);
            else
                ImGuiNET.ImGui.GetWindowDrawList().AddText((ImFontPtr)Font, Font.Size, textPos, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text), Text);
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
