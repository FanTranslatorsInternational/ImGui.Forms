using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGuiNET;
using Veldrid;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls.Lists
{
    public abstract class BaseList<TItem> : Component
        where TItem : Component
    {
        private bool _scrollToLast;

        public IList<TItem> Items { get; protected set; }
        public Alignment Alignment { get; set; } = Alignment.Vertical;

        public Size Size { get; set; } = Size.Content;
        public int ItemSpacing { get; set; }
        public Vector2 Padding { get; set; }
        public bool ScrollToLastItem { get; set; }

        public override Size GetSize() => Size;

        public BaseList()
        {
            var items = new ObservableList<TItem>();

            items.ItemInserted += Items_NewItem;
            items.ItemAdded += Items_NewItem;
            items.ItemSet += Items_ItemSet;
            items.ItemRemoved += Items_ItemRemoved;

            Items = items;
        }

        protected override int GetContentWidth(int parentWidth, float layoutCorrection = 1)
        {
            var widths = Items.Select(x => x.GetWidth(parentWidth, layoutCorrection)).DefaultIfEmpty(Size.Width.IsParentAligned ? parentWidth : 0).ToArray();

            var totalWidth = Alignment == Alignment.Horizontal ?
                widths.Sum() + Math.Max(0, Items.Count - 1) * ItemSpacing :
                widths.Max();
            return Math.Min(parentWidth, (int)(totalWidth + Padding.X * 2));
        }

        protected override int GetContentHeight(int parentHeight, float layoutCorrection = 1)
        {
            var heights = Items.Select(x => x.GetHeight(parentHeight, layoutCorrection)).DefaultIfEmpty(Size.Height.IsParentAligned ? parentHeight : 0).ToArray();

            var totalHeight = Alignment == Alignment.Vertical ?
                heights.Sum() + Math.Max(0, Items.Count - 1) * ItemSpacing :
                heights.Max();
            return Math.Min(parentHeight, (int)(totalHeight + Padding.Y * 2));
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var localItems = Items?.ToArray() ?? Array.Empty<Component>();

            var listDimension = localItems.Sum(i => GetDimension(i, contentRect)) + Math.Max(0, localItems.Length - 1) * ItemSpacing + (int)(GetPadding() * 2);
            var scrollableDimension = GetScrollableDimension(contentRect);

            if (ImGuiNET.ImGui.BeginChild($"{Id}", contentRect.Size, ImGuiChildFlags.None))
            {
                if (_scrollToLast)
                {
                    SetScroll(listDimension - scrollableDimension);
                    _scrollToLast = false;
                }

                var scrollbarDimension = listDimension > scrollableDimension ? (int)ImGuiNET.ImGui.GetStyle().ScrollbarSize : 0;
                var (scrollX, scrollY) = (-(int)ImGuiNET.ImGui.GetScrollX(), -(int)ImGuiNET.ImGui.GetScrollY());

                var (x, y) = (Padding.X, Padding.Y);

                if (ImGuiNET.ImGui.BeginChild($"{Id}_in", GetInnerSize(listDimension, contentRect)))
                {
                    for (var i = 0; i < localItems.Length; i++)
                    {
                        var itemWidth = localItems[i].GetWidth(contentRect.Width - (Alignment == Alignment.Vertical ? scrollbarDimension : 0) - (int)(Padding.X * 2));
                        var itemHeight = localItems[i].GetHeight(contentRect.Height - (Alignment == Alignment.Horizontal ? scrollbarDimension : 0) - (int)(Padding.Y * 2));

                        ImGuiNET.ImGui.SetCursorPos(new Vector2(x, y));
                        if (ImGuiNET.ImGui.BeginChild($"{Id}_itm{i}", new Vector2(itemWidth, itemHeight)))
                            localItems[i].Update(new Rectangle((int)(contentRect.X + x + scrollX), (int)(contentRect.Y + y + scrollY), itemWidth, itemHeight));

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

        protected override void SetTabInactiveCore()
        {
            foreach (TItem item in Items)
                item?.SetTabInactiveInternal();
        }

        #region Selection methods

        private int GetDimension(Component component, Rectangle contentRect)
        {
            return Alignment == Alignment.Vertical
                ? component.GetHeight(contentRect.Height)
                : component.GetWidth(contentRect.Width);
        }

        private float GetPadding()
        {
            return Alignment == Alignment.Vertical
                ? Padding.Y
                : Padding.X;
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

        private void SetScroll(float scroll)
        {
            scroll = Math.Max(0, scroll);

            if (Alignment == Alignment.Vertical)
                ImGuiNET.ImGui.SetScrollY(scroll);
            else
                ImGuiNET.ImGui.SetScrollX(scroll);
        }

        #endregion

        #region Observable events

        private void Items_NewItem(object sender, ItemEventArgs<TItem> e)
        {
            OnItemAdded(e);
        }

        private void Items_ItemSet(object sender, ItemSetEventArgs<TItem> e)
        {
            OnItemSet(e);
        }

        private void Items_ItemRemoved(object sender, ItemEventArgs<TItem> e)
        {
            OnItemRemoved(e);
        }

        protected virtual void OnItemAdded(ItemEventArgs<TItem> e)
        {
            _scrollToLast = ScrollToLastItem;
        }

        protected virtual void OnItemSet(ItemSetEventArgs<TItem> e) { }

        protected virtual void OnItemRemoved(ItemEventArgs<TItem> e) { }

        #endregion
    }
}
