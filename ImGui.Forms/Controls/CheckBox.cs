using System;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class CheckBox : Component
    {
        private bool _checked;

        #region Properties

        public LocalizedString Text { get; set; }
        public LocalizedString Tooltip { get; set; }
        public FontResource Font { get; set; }

        public bool Checked
        {
            get => _checked;
            set
            {
                _checked = value;
                OnCheckChanged();
            }
        }

        #endregion

        #region Events

        public event EventHandler CheckChanged;

        #endregion

        public CheckBox(LocalizedString text = default)
        {
            Text = text;
        }

        public override Size GetSize()
        {
            ApplyStyles(Enabled, Font);

            var size = TextMeasurer.MeasureText(Text);
            var width = (int)(Math.Ceiling(size.X) + 21 + ImGuiNET.ImGui.GetStyle().ItemInnerSpacing.X);
            var height = (int)Math.Max(Math.Ceiling(size.Y), 21);

            RemoveStyles(Enabled, Font);

            return new Size(width, height);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            ImGuiNET.ImGui.SetNextItemWidth(contentRect.Width);

            var check = Checked;
            var enabled = Enabled;
            var font = Font;

            ApplyStyles(enabled, font);

            if (IsHovering(contentRect) && !string.IsNullOrEmpty(Tooltip))
                ImGuiNET.ImGui.SetTooltip(Tooltip);

            if (ImGuiNET.ImGui.Checkbox(Text, ref check) && Enabled)
                Checked = check;

            RemoveStyles(enabled, font);
        }

        private void ApplyStyles(bool enabled, FontResource font)
        {
            if (!enabled)
            {
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.CheckMark, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            }

            ImFontPtr? fontPtr = font?.GetPointer();
            if (fontPtr != null)
                ImGuiNET.ImGui.PushFont(fontPtr.Value);
        }

        private void RemoveStyles(bool enabled, FontResource font)
        {
            if (font?.GetPointer() != null)
                ImGuiNET.ImGui.PopFont();

            if (!enabled)
                ImGuiNET.ImGui.PopStyleColor(4);
        }

        private bool IsHovering(Rectangle contentRect)
        {
            return ImGuiNET.ImGui.IsMouseHoveringRect(contentRect.Position, contentRect.Position + contentRect.Size);
        }

        private void OnCheckChanged()
        {
            CheckChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
