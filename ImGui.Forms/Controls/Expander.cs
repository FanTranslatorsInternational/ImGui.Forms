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
        #region Properties

        public LocalizedString Caption { get; set; }
        public Size Size { get; set; } = Size.WidthAlign;

        public Component Content { get; set; }

        public bool Expanded { get; set; }

        #endregion

        #region Events

        public event EventHandler ExpandedChanged;

        #endregion

        public Expander(Component content, LocalizedString caption = default)
        {
            Content = content;
            Caption = caption;
        }

        public override Size GetSize()
        {
            SizeValue height = Size.Height.IsContentAligned
                    ? Expanded
                        ? SizeValue.Absolute((int)(GetHeaderHeight() + ImGuiNET.ImGui.GetStyle().ItemSpacing.X + 200))
                        : SizeValue.Absolute(GetHeaderHeight())
                    : Size.Height;

            return new Size(Size.Width, height);
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
            var size = TextMeasurer.MeasureText(Caption);
            var framePadding = ImGuiNET.ImGui.GetStyle().FramePadding;
            return (int)Math.Ceiling(size.Y + framePadding.Y * 2);
        }
    }
}
