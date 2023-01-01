using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class Button : Component
    {
        #region Properties

        public LocalizedString Text { get; set; }

        public Vector2 Padding { get; set; } = new Vector2(2, 2);

        public SizeValue Width { get; set; } = SizeValue.Content;

        public FontResource Font { get; set; }

        #endregion

        #region Events

        public event EventHandler Clicked;

        #endregion

        public override Size GetSize()
        {
            ApplyStyles(Enabled, Font);

            var textSize = FontResource.MeasureText(EscapeText());
            SizeValue width = Width.IsContentAligned ? (int)Math.Ceiling(textSize.X) + (int)Padding.X * 2 : Width;
            var height = (int)Math.Ceiling(textSize.Y) + (int)Padding.Y * 2;

            RemoveStyles(Enabled, Font);

            return new Size(width, height);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var enabled = Enabled;
            var font = Font;

            ApplyStyles(enabled, font);

            if (ImGuiNET.ImGui.Button(EscapeText(), new Vector2(contentRect.Width, contentRect.Height)) && Enabled)
                OnClicked();

            RemoveStyles(enabled, font);
        }

        private void ApplyStyles(bool enabled, FontResource font)
        {
            if (!enabled)
            {
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, 0xFF666666);
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF666666);
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF666666);
            }

            if (font != null)
                ImGuiNET.ImGui.PushFont((ImFontPtr)Font);

            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Padding);
        }

        private void RemoveStyles(bool enabled, FontResource font)
        {
            ImGuiNET.ImGui.PopStyleVar();

            if (font != null)
                ImGuiNET.ImGui.PopFont();

            if (!enabled)
                ImGuiNET.ImGui.PopStyleColor(3);
        }

        protected void OnClicked()
        {
            Clicked?.Invoke(this, new EventArgs());
        }

        protected string EscapeText()
        {
            return Text.ToString().Replace("\\n", Environment.NewLine);
        }
    }
}
