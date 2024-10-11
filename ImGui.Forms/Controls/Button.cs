using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class Button : Component
    {
        #region Properties

        public LocalizedString Text { get; set; }
        public LocalizedString Tooltip { get; set; }
        public FontResource Font { get; set; }

        public KeyCommand KeyAction { get; set; }

        public Vector2 Padding { get; set; } = new(2, 2);

        public SizeValue Width { get; set; } = SizeValue.Content;

        #endregion

        #region Events

        public event EventHandler Clicked;

        #endregion

        public Button(LocalizedString text = default)
        {
            Text = text;
        }

        public override Size GetSize()
        {
            ApplyStyles(Enabled, Font);

            var textSize = TextMeasurer.MeasureText(EscapeText());
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

            if ((ImGuiNET.ImGui.Button(EscapeText(), contentRect.Size) || IsKeyDown(KeyAction)) && Enabled)
                OnClicked();

            if (Tooltip is { IsEmpty: false } && IsHoveredCore())
            {
                ImGuiNET.ImGui.BeginTooltip();
                ImGuiNET.ImGui.Text(Tooltip);
                ImGuiNET.ImGui.EndTooltip();
            }

            RemoveStyles(enabled, font);
        }

        private void ApplyStyles(bool enabled, FontResource font)
        {
            if (!enabled)
            {
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            }

            ImFontPtr? fontPtr = font?.GetPointer();
            if (fontPtr != null)
                ImGuiNET.ImGui.PushFont(fontPtr.Value);

            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Padding);
        }

        private void RemoveStyles(bool enabled, FontResource font)
        {
            ImGuiNET.ImGui.PopStyleVar();
            
            if (Font?.GetPointer() != null)
                ImGuiNET.ImGui.PopFont();

            if (!enabled)
                ImGuiNET.ImGui.PopStyleColor(3);
        }

        protected void OnClicked()
        {
            Clicked?.Invoke(this, EventArgs.Empty);
        }

        protected string EscapeText()
        {
            return Text.ToString().Replace("\\n", Environment.NewLine);
        }
    }
}
