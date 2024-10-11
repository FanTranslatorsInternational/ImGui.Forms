using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Resources;
using ImGuiNET;
using SixLabors.ImageSharp;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls
{
    public class ProgressBar : Component
    {
        #region Properties

        public LocalizedString Text { get; set; }

        public FontResource Font { get; set; }

        public Size Size { get; set; } = Size.Parent;

        public ThemedColor ProgressColor { get; set; } = Color.FromRgba(0x27, 0xBB, 0x65, 0xFF);

        public int Minimum { get; set; }

        public int Maximum { get; set; } = 100;

        public int Value { get; set; }

        #endregion

        public override Size GetSize() => Size;

        protected override void UpdateInternal(Rectangle contentRect)
        {
            // Draw progress bar
            var barWidth = (float)Math.Ceiling((float)contentRect.Width / Maximum * Value);
            ImGuiNET.ImGui.GetWindowDrawList().AddRectFilled(new Vector2(contentRect.X, contentRect.Y), new Vector2(contentRect.X + barWidth, contentRect.Y + contentRect.Height), ProgressColor.ToUInt32());

            // Draw border
            ImGuiNET.ImGui.GetWindowDrawList().AddRect(new Vector2(contentRect.X, contentRect.Y), new Vector2(contentRect.X + contentRect.Width, contentRect.Y + contentRect.Height), ImGuiNET.ImGui.GetColorU32(ImGuiCol.Border));

            // Draw text
            var textSize = TextMeasurer.MeasureText(Text);
            var textPos = new Vector2(contentRect.X + (contentRect.Width - textSize.X) / 2, contentRect.Y + (contentRect.Height - textSize.Y) / 2);

            ImFontPtr? fontPtr = Font?.GetPointer();
            if (fontPtr != null)
                ImGuiNET.ImGui.GetWindowDrawList().AddText(fontPtr.Value, Font.Data.Size, textPos, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text), Text);
            else
                ImGuiNET.ImGui.GetWindowDrawList().AddText(textPos, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text), Text);
        }

        protected override void ApplyStyles()
        {
            ImFontPtr? fontPtr = Font?.GetPointer();
            if (fontPtr != null)
                ImGuiNET.ImGui.PushFont(fontPtr.Value);
        }

        protected override void RemoveStyles()
        {
            if (Font?.GetPointer() != null)
                ImGuiNET.ImGui.PopFont();
        }
    }
}
