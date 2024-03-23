using System;
using System.Drawing;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
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
        public LocalizedString Text { get; set; }

        public FontResource Font { get; set; }

        public float LineDistance { get; set; }

        public Color TextColor { get; set; } = Color.Empty;

        public SizeValue Width { get; set; } = SizeValue.Content;

        public override Size GetSize()
        {
            ApplyStyles();

            var escapedText = EscapeText();
            var lines = escapedText.Split(Environment.NewLine);

            var textSize = Vector2.Zero;
            foreach (var line in lines)
            {
                var lineSize = FontResource.MeasureText(line, true);
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

                var lineSize = FontResource.MeasureText(line, true);
                pos += new Vector2(0, lineSize.Y + LineDistance);
            }

            RemoveStyles();
        }

        protected override void ApplyStyles()
        {
            if (TextColor != Color.Empty)
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Text, TextColor.ToUInt32());

            if (Font != null)
                ImGuiNET.ImGui.PushFont((ImFontPtr)Font);
        }

        protected override void RemoveStyles()
        {
            if (Font != null)
                ImGuiNET.ImGui.PopFont();

            if (TextColor != Color.Empty)
                ImGuiNET.ImGui.PopStyleColor();
        }

        protected string EscapeText()
        {
            return Text.ToString().Replace("\n", Environment.NewLine);
        }
    }
}
