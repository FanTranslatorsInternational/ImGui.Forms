using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using Veldrid;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls.Lists
{
    public abstract class BaseList<TItem> : Component
        where TItem : Component
    {
        public IList<TItem> Items { get; protected set; } = new List<TItem>();
        public Alignment Alignment { get; set; } = Alignment.Vertical;

        public Size Size { get; set; } = Size.Content;
        public int ItemSpacing { get; set; }

        public override Size GetSize() => Size;

        protected override int GetContentWidth(int parentWidth, float layoutCorrection = 1)
        {
            var widths = Items.Select(x => x.GetWidth(parentWidth, layoutCorrection)).DefaultIfEmpty(Size.Width.IsParentAligned ? parentWidth : 0);

            var totalWidth = Alignment == Alignment.Horizontal ?
                widths.Sum() + Math.Max(0, Items.Count - 1) * ItemSpacing :
                widths.Max();
            return Math.Min(parentWidth, totalWidth);
        }

        protected override int GetContentHeight(int parentHeight, float layoutCorrection = 1)
        {
            var heights = Items.Select(x => x.GetHeight(parentHeight, layoutCorrection)).DefaultIfEmpty(Size.Height.IsParentAligned ? parentHeight : 0);

            var totalHeight = Alignment == Alignment.Vertical ?
                heights.Sum() + Math.Max(0, Items.Count - 1) * ItemSpacing :
                heights.Max();
            return Math.Min(parentHeight, totalHeight);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var localItems = Items?.ToArray() ?? Array.Empty<Component>();

            var listDimension = localItems.Sum(i => GetDimension(i, contentRect)) + Math.Max(0, localItems.Length - 1) * ItemSpacing;
            var scrollableDimension = GetScrollableDimension(contentRect);

            if (ImGuiNET.ImGui.BeginChild($"{Id}", contentRect.Size, false))
            {
                var scrollbarDimension = listDimension > scrollableDimension ? (int)ImGuiNET.ImGui.GetStyle().ScrollbarSize : 0;
                var (scrollX, scrollY) = (-(int)ImGuiNET.ImGui.GetScrollX(), -(int)ImGuiNET.ImGui.GetScrollY());

                var (x, y) = (0, 0);

                if (ImGuiNET.ImGui.BeginChild($"{Id}_in", GetInnerSize(listDimension, contentRect)))
                {
                    for (var i = 0; i < localItems.Length; i++)
                    {
                        var itemWidth = localItems[i].GetWidth(contentRect.Width - (Alignment == Alignment.Vertical ? scrollbarDimension : 0));
                        var itemHeight = localItems[i].GetHeight(contentRect.Height - (Alignment == Alignment.Horizontal ? scrollbarDimension : 0));

                        ImGuiNET.ImGui.SetCursorPos(new Vector2(x, y));
                        if (ImGuiNET.ImGui.BeginChild($"{Id}_itm{i}", new Vector2(itemWidth, itemHeight)))
                            localItems[i].Update(new Rectangle(contentRect.X + x + scrollX, contentRect.Y + y + scrollY, itemWidth, itemHeight));

                        ImGuiNET.ImGui.EndChild();

                        if (Alignment == Alignment.Vertical)
                            y += itemHeight + ItemSpacing;
                        else
                            x += itemWidth + ItemSpacing;
                    }
                }

                ImGuiNET.ImGui.EndChild();
            }

            ImGuiNET.ImGui.EndChild();
        }

        private int GetDimension(Component component, Rectangle contentRect)
        {
            return Alignment == Alignment.Vertical
                ? component.GetHeight(contentRect.Height)
                : component.GetWidth(contentRect.Width);
        }

        private int GetScrollableDimension(Rectangle contentRect)
        {
            return Alignment == Alignment.Vertical
                ? contentRect.Height
                : contentRect.Width;
        }

        private Vector2 GetInnerSize(int listDimension, Rectangle contentRect)
        {
            return Alignment == Alignment.Vertical ?
                new Vector2(contentRect.Width, listDimension) :
                new Vector2(listDimension, contentRect.Height);
        }
    }
}
