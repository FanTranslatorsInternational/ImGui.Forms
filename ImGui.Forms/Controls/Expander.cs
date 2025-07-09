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
    public class Expander : Component
    {
        #region Properties

        public LocalizedString Caption { get; set; }
        public Size Size { get; set; } = Size.WidthAlign;

        public int WidthIndent { get; set; } = 5;

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
                    ? SizeValue.Content
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
                int contentPosY = GetContentPosY();
                if (contentPosY <= contentRect.Height)
                {
                    if (ImGuiNET.ImGui.BeginChild($"{Id}-in"))
                    {
                        ImGuiNET.ImGui.SetCursorPos(new Vector2(WidthIndent, 0));
                        Content?.Update(new Rectangle(contentRect.X + WidthIndent, contentRect.Y + contentPosY, contentRect.Width, contentRect.Height - contentPosY));
                    }

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

        protected override int GetContentHeight(int parentWidth, int parentHeight, float layoutCorrection = 1)
        {
            if (!Expanded)
                return GetHeaderHeight();

            int height = Content.GetHeight(parentWidth, parentHeight, layoutCorrection);
            if (height <= 0)
                return GetHeaderHeight();

            return height + GetHeaderHeight() + (int)ImGuiNET.ImGui.GetStyle().ItemSpacing.Y;
        }

        private void OnExpandedChanged()
        {
            ExpandedChanged?.Invoke(this, EventArgs.Empty);
        }

        private int GetContentPosY()
        {
            int height = GetHeaderHeight();

            if (Expanded)
                height += (int)ImGuiNET.ImGui.GetStyle().ItemSpacing.Y;

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
