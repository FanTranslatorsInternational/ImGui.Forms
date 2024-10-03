using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls
{
    public class Label : Component
    {
        #region Properties

        public LocalizedString Text { get; set; }

        public FontResource Font { get; set; }

        public float LineDistance { get; set; }

        public ThemedColor TextColor { get; set; }

        public SizeValue Width { get; set; } = SizeValue.Content;

        #endregion

        public Label(LocalizedString text = default)
        {
            Text = text;
        }

        public override Size GetSize()
        {
            ApplyStyles();

            var escapedText = EscapeText();
            var lines = escapedText.Split(Environment.NewLine);

            var textSize = Vector2.Zero;
            foreach (var line in lines)
            {
                var lineSize = TextMeasurer.MeasureText(line, true);
                textSize = new Vector2(Math.Max(textSize.X, lineSize.X), textSize.Y + lineSize.Y);
            }
            SizeValue width = Width.IsContentAligned ? (int)Math.Ceiling(textSize.X) : Width;
            var height = (int)Math.Ceiling(textSize.Y + (lines.Length - 1) * LineDistance);

            RemoveStyles();

            return new Size(width, height);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            ApplyStyles();

            var escapedText = EscapeText();

            var pos = contentRect.Position;
            foreach (var line in escapedText.Split(Environment.NewLine))
            {
                ImGuiNET.ImGui.GetWindowDrawList().AddText(pos, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text), line);

                var lineSize = TextMeasurer.MeasureText(line, true);
                pos += new Vector2(0, lineSize.Y + LineDistance);
            }

            RemoveStyles();
        }

        protected override void ApplyStyles()
        {
            if (!TextColor.IsEmpty)
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Text, TextColor.ToUInt32());

            ImFontPtr? fontPtr = Font?.GetPointer();
            if (fontPtr != null)
                ImGuiNET.ImGui.PushFont(fontPtr.Value);
        }

        protected override void RemoveStyles()
        {
            if (Font?.GetPointer() != null)
                ImGuiNET.ImGui.PopFont();

            if (!TextColor.IsEmpty)
                ImGuiNET.ImGui.PopStyleColor();
        }

        protected string EscapeText()
        {
            return Text.ToString().Replace("\n", Environment.NewLine);
        }
    }
}
