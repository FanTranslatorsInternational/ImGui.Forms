using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls
{
    public class RadioButtonGroup : Component
    {
        private static readonly Vector2 RadioSize = new(19, 19);

        public IList<RadioButtonItem> Items { get; } = new List<RadioButtonItem>();

        public RadioButtonItem SelectedItem { get; set; }

        public Alignment Alignment { get; set; } = Alignment.Horizontal;

        public event EventHandler SelectedItemChanged;

        public override Size GetSize()
        {
            float totalWidth = 0;
            float totalHeight = 0;

            Vector2 itemSpacing = ImGuiNET.ImGui.GetStyle().ItemSpacing;

            switch (Alignment)
            {
                case Alignment.Horizontal:
                    foreach (RadioButtonItem item in Items)
                    {
                        Vector2 itemSize = GetItemSize(item);

                        totalWidth += itemSize.X;
                        if (totalHeight < itemSize.Y)
                            totalHeight = itemSize.Y;
                    }
                    totalWidth += Math.Max(0, Items.Count) * itemSpacing.X;

                    return new Size(SizeValue.Absolute((int)totalWidth), SizeValue.Absolute((int)totalHeight));

                case Alignment.Vertical:
                    foreach (RadioButtonItem item in Items)
                    {
                        Vector2 itemSize = GetItemSize(item);

                        totalHeight += itemSize.Y;
                        if (totalWidth < itemSize.X)
                            totalWidth = itemSize.X;
                    }
                    totalWidth += Math.Max(0, Items.Count) * itemSpacing.Y;

                    return new Size(SizeValue.Absolute((int)totalWidth), SizeValue.Absolute((int)totalHeight));

                default:
                    return Size.Content;
            }
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            RadioButtonItem selected = SelectedItem;

            foreach (RadioButtonItem item in Items)
            {
                if (ImGuiNET.ImGui.RadioButton(item.Text, item == SelectedItem))
                    selected = item;

                if (Alignment is Alignment.Horizontal && item != Items[^1])
                    ImGuiNET.ImGui.SameLine();
            }

            bool isChanged = SelectedItem == selected;

            SelectedItem = selected;

            if (isChanged)
                OnSelectedItemChanged();
        }

        private static Vector2 GetItemSize(RadioButtonItem item)
        {
            Vector2 framePadding = Style.GetStyleVector2(ImGuiStyleVar.FramePadding);
            Vector2 itemInnerSpacing = Style.GetStyleVector2(ImGuiStyleVar.ItemInnerSpacing);

            Vector2 textSize = TextMeasurer.MeasureText(item.Text);

            float width = framePadding.X * 2 + RadioSize.X + itemInnerSpacing.X + textSize.X;
            float height = Math.Max(framePadding.Y * 2 + RadioSize.Y, textSize.Y);

            return new Vector2(width, height);
        }

        private void OnSelectedItemChanged()
        {
            SelectedItemChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class RadioButtonItem
    {
        public LocalizedString Text { get; set; }

        public RadioButtonItem(LocalizedString text = default)
        {
            Text = text;
        }
    }
}
