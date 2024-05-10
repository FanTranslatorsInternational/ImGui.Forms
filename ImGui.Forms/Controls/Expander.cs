using System;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class Expander : Component
    {
        public LocalizedString Caption { get; set; }

        public Component Content { get; set; }

        public int ContentHeight { get; set; } = 200;

        public bool Expanded { get; set; }

        #region Events

        public event EventHandler ExpandedChanged;

        #endregion

        public override Size GetSize()
        {
            return new Size(1f, (int)(GetHeaderHeight() + (Expanded ? ImGuiNET.ImGui.GetStyle().ItemSpacing.X + ContentHeight : 0)));
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var expanded = Expanded;
            var flags = expanded ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;

            expanded = ImGuiNET.ImGui.CollapsingHeader(Caption, flags);
            if (expanded)
            {
                if (ImGuiNET.ImGui.BeginChild($"{Id}-in"))
                {
                    Content?.Update(new Rectangle(contentRect.X, contentRect.Y + GetContentPosY(), contentRect.Width, contentRect.Height - GetContentPosY()));

                    ImGuiNET.ImGui.EndChild();
                }
            }

            if (Expanded != expanded)
            {
                Expanded = expanded;
                OnExpandedChanged();
            }
        }

        protected override void SetTabInactiveCore()
        {
            Content?.SetTabInactiveInternal();
        }

        private void OnExpandedChanged()
        {
            ExpandedChanged?.Invoke(this, EventArgs.Empty);
        }

        private int GetContentPosY()
        {
            int height = GetHeaderHeight();
            if (Expanded)
                height += (int)ImGuiNET.ImGui.GetStyle().ItemSpacing.X;

            return height;
        }

        private int GetHeaderHeight()
        {
            var size = FontResource.MeasureText(Caption);
            var framePadding = ImGuiNET.ImGui.GetStyle().FramePadding;
            return (int)Math.Ceiling(size.Y + framePadding.Y * 2);
        }
    }
}
