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
        public LocalizedString Caption { get; set; }

        public LocalizedString Tooltip { get; set; }

        public bool Checked { get; set; }

        #region Events

        public event EventHandler CheckChanged;

        #endregion

        public override Size GetSize()
        {
            var size = FontResource.MeasureText(Caption);
            return new Size((int)(Math.Ceiling(size.X) + 21 + ImGuiNET.ImGui.GetStyle().ItemInnerSpacing.X), (int)Math.Max(Math.Ceiling(size.Y), 21));
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            // TODO: Draw checkbox manually, when disabled, to work around checkmark flickering
            ImGuiNET.ImGui.SetNextItemWidth(contentRect.Width);

            var check = Checked;
            var enabled = Enabled;

            ApplyStyles(enabled);

            if (IsHovering(contentRect) && !string.IsNullOrEmpty(Tooltip))
                ImGuiNET.ImGui.SetTooltip(Tooltip);

            if (ImGuiNET.ImGui.Checkbox(Caption, ref check) && Enabled)
            {
                Checked = check;
                OnCheckChanged();
            }

            RemoveStyles(enabled);
        }

        private void ApplyStyles(bool enabled)
        {
            if (!enabled)
            {
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.CheckMark, 0xFF999999);
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.FrameBg, 0xFF666666);
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.FrameBgActive, 0xFF666666);
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, 0xFF666666);
            }
        }

        private void RemoveStyles(bool enabled)
        {
            if (!enabled)
                ImGuiNET.ImGui.PopStyleColor(4);
        }

        private bool IsHovering(Rectangle contentRect)
        {
            return ImGuiNET.ImGui.IsMouseHoveringRect(contentRect.Position, contentRect.Position + contentRect.Size);
        }

        private void OnCheckChanged()
        {
            CheckChanged?.Invoke(this, new EventArgs());
        }
    }
}
