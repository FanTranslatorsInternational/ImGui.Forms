using System;
using System.Drawing;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Models;
using ImGuiNET;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls
{
    public class Label : Component
    {
        public string Caption { get; set; } = string.Empty;

        public FontResource Font { get; set; }

        public Color TextColor { get; set; } = Color.Empty;

        public SizeValue Width { get; set; } = SizeValue.Absolute(-1);

        public override Size GetSize()
        {
            ApplyStyles();

            var textSize = ImGuiNET.ImGui.CalcTextSize(Caption ?? string.Empty);
            SizeValue width = Width.Value == -1 ? (int)Math.Ceiling(textSize.X) : Width;
            var height = (int)Math.Ceiling(textSize.Y);

            RemoveStyles();

            return new Size(width, height);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            ImGuiNET.ImGui.SetNextItemWidth(contentRect.Width);
            ImGuiNET.ImGui.Text(Caption);
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
    }
}
