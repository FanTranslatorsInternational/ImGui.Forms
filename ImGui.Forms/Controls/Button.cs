using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class Button : Component
    {
        #region Properties

        public string Caption { get; set; } = string.Empty;

        public Vector2 Padding { get; set; } = new Vector2(2, 2);

        public SizeValue Width { get; set; } = SizeValue.Absolute(-1);

        public FontResource FontResource { get; set; }

        public bool Enabled { get; set; } = true;

        #endregion

        #region Events

        public event EventHandler Clicked;

        #endregion

        public override Size GetSize()
        {
            ApplyStyles(Enabled, FontResource);

            var textSize = ImGuiNET.ImGui.CalcTextSize(Caption ?? string.Empty);
            SizeValue width = (int)Width.Value == -1 ? (int)Math.Ceiling(textSize.X) + (int)Padding.X * 2 : Width;
            var height = (int)Padding.Y * 2 + (int)Math.Ceiling(textSize.Y);

            RemoveStyles(Enabled, FontResource);

            return new Size(width, height);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var enabled = Enabled;
            var font = FontResource;

            ApplyStyles(enabled, font);

            if (ImGuiNET.ImGui.Button(Caption ?? string.Empty, new Vector2(contentRect.Width, contentRect.Height)) && Enabled)
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
                ImGuiNET.ImGui.PushFont((ImFontPtr)FontResource);

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

        private void OnClicked()
        {
            Clicked?.Invoke(this, new EventArgs());
        }
    }
}
