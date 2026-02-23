using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Support;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls.Lists;

public class List<TItem> : Component where TItem : Component
{
    private bool _scrollToLast;

    #region Properties

    public IList<TItem> Items { get; protected set; }
    public TItem SelectedItem { get; set; }

    public Alignment Alignment { get; set; } = Alignment.Vertical;

    public Size Size { get; set; } = Size.Content;

    public int ItemSpacing { get; set; }
    public Vector2 Padding { get; set; }

    public bool ScrollToLastItem { get; set; }

    public bool IsSelectable { get; set; } = false;

    #endregion

    #region Events

    public event EventHandler SelectedItemChanged;

    #endregion

    public List()
    {
        var items = new ObservableList<TItem>();

        items.ItemInserted += Items_NewItem;
        items.ItemAdded += Items_NewItem;
        items.ItemSet += Items_ItemSet;
        items.ItemRemoved += Items_ItemRemoved;

        Items = items;
    }

    public override Size GetSize() => Size;

    protected override void UpdateInternal(Rectangle contentRect)
    {
        TItem selectedItem = null;
        var localItems = Items?.ToArray() ?? Array.Empty<TItem>();

        var listDimension = localItems.Sum(i => GetDimension(i, contentRect)) + Math.Max(0, localItems.Length - 1) * ItemSpacing + (int)(GetPadding() * 2);
        var scrollableDimension = GetScrollableDimension(contentRect);

        var flags = ImGuiWindowFlags.None;
        if (Alignment == Alignment.Horizontal)
            flags |= ImGuiWindowFlags.HorizontalScrollbar;

        if (Hexa.NET.ImGui.ImGui.BeginChild($"{Id}", contentRect.Size, ImGuiChildFlags.None, flags))
        {
            if (_scrollToLast)
            {
                SetScroll(listDimension - scrollableDimension);
                _scrollToLast = false;
            }

            var scrollbarDimension = listDimension > scrollableDimension ? (int)Hexa.NET.ImGui.ImGui.GetStyle().ScrollbarSize : 0;
            var scroll = new Vector2(-(int)Hexa.NET.ImGui.ImGui.GetScrollX(), -(int)Hexa.NET.ImGui.ImGui.GetScrollY());

            var (x, y) = (Padding.X, Padding.Y);
            var contentPos = new Vector2(contentRect.X + x, contentRect.Y + y);

            if (Hexa.NET.ImGui.ImGui.BeginChild($"{Id}_in", GetInnerSize(listDimension, contentRect), ImGuiChildFlags.None))
            {
                for (var i = 0; i < localItems.Length; i++)
                {
                    var item = localItems[i];

                    var itemWidth = item.GetWidth((int)contentRect.Width - (Alignment == Alignment.Vertical ? scrollbarDimension : 0) - (int)(Padding.X * 2), (int)contentRect.Height);
                    var itemHeight = item.GetHeight((int)contentRect.Width, (int)contentRect.Height - (Alignment == Alignment.Horizontal ? scrollbarDimension : 0) - (int)(Padding.Y * 2));
                    var itemSize = new Vector2(itemWidth, itemHeight);

                    var contentItemPos = new Vector2(contentRect.X + x, contentRect.Y + y);
                    var contentItemScrollPos = contentItemPos + scroll;
                    var contentItemScrollEndPos = contentItemScrollPos + itemSize;

                    var contentScrollPos = contentPos + scroll;

                    // Create item states
                    var isItemSelected = item == SelectedItem;
                    var isItemHovered = Hexa.NET.ImGui.ImGui.IsMouseHoveringRect(contentRect.Position, contentRect.Position + contentRect.Size)
                                        && (Alignment == Alignment.Vertical
                                            ? (int)(Hexa.NET.ImGui.ImGui.GetMousePos().Y - contentScrollPos.Y) / (itemHeight + ItemSpacing) == i
                                            : (int)(Hexa.NET.ImGui.ImGui.GetMousePos().X - contentScrollPos.X) / (itemWidth + ItemSpacing) == i);
                    var isItemClicked = isItemHovered && Hexa.NET.ImGui.ImGui.IsMouseDown(ImGuiMouseButton.Left);

                    // Set selected item locally
                    if (IsSelectable && isItemHovered && Hexa.NET.ImGui.ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        selectedItem = item;

                    // Add background color for selection
                    var color = isItemClicked
                        ? Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.HeaderActive)
                        : isItemHovered
                            ? Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.HeaderHovered)
                            : isItemSelected
                                ? Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.Header)
                                : 0;

                    if (IsSelectable && (isItemHovered || isItemSelected))
                        Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRectFilled(contentItemScrollPos, contentItemScrollEndPos, color);

                    Hexa.NET.ImGui.ImGui.SetCursorPos(new Vector2(x, y));
                    if (Hexa.NET.ImGui.ImGui.BeginChild($"{Id}_itm{i}", itemSize))
                        item.Update(new Rectangle(contentItemScrollPos, new Vector2(itemWidth, itemHeight)));

                    Hexa.NET.ImGui.ImGui.EndChild();

                    if (Alignment == Alignment.Vertical)
                        y += itemHeight + ItemSpacing;
                    else
                        x += itemWidth + ItemSpacing;
                }
            }

            Hexa.NET.ImGui.ImGui.EndChild();
        }

        Hexa.NET.ImGui.ImGui.EndChild();

        // Invoke selected item change event
        if (IsSelectable && selectedItem != null && SelectedItem != selectedItem)
        {
            SelectedItem = selectedItem;
            OnSelectedItemChanged();
        }
    }

    protected override int GetContentWidth(int parentWidth, int parentHeight, float layoutCorrection = 1f)
    {
        // If no items, return 0
        if (Items.Count <= 0)
            return 0;

        int totalWidth = GetTotalWidth(parentWidth, parentHeight, layoutCorrection);
        int totalHeight = GetTotalHeight(parentWidth, parentHeight, layoutCorrection);

        // Allocate for scroll bar
        if (Alignment == Alignment.Vertical && totalHeight > GetHeight(parentWidth, parentHeight, layoutCorrection))
            totalWidth += (int)Hexa.NET.ImGui.ImGui.GetStyle().ScrollbarSize;

        return Math.Min(parentWidth, (int)(totalWidth + Padding.X * 2));
    }

    private int GetTotalWidth(int parentWidth, int parentHeight, float layoutCorrection)
    {
        int[] widths = Items.Select(x => x.GetWidth(parentWidth, parentHeight, layoutCorrection)).ToArray();

        // Base on alignment, sum widths or take maximum
        return Alignment == Alignment.Horizontal ?
            widths.Sum() + (Items.Count - 1) * ItemSpacing :
            widths.Max();
    }

    protected override int GetContentHeight(int parentWidth, int parentHeight, float layoutCorrection = 1f)
    {
        // If no items, return 0
        if (Items.Count <= 0)
            return 0;

        int totalWidth = GetTotalWidth(parentWidth, parentHeight, layoutCorrection);
        int totalHeight = GetTotalHeight(parentWidth, parentHeight, layoutCorrection);

        // Allocate for scroll bar
        if (Alignment == Alignment.Horizontal && totalWidth > GetWidth(parentWidth, parentHeight, layoutCorrection))
            totalHeight += (int)Hexa.NET.ImGui.ImGui.GetStyle().ScrollbarSize;

        return Math.Min(parentHeight, (int)(totalHeight + Padding.X * 2));
    }

    private int GetTotalHeight(int parentWidth, int parentHeight, float layoutCorrection)
    {
        int[] heights = Items.Select(x => x.GetHeight(parentWidth, parentHeight, layoutCorrection)).ToArray();

        // Base on alignment, sum heights or take maximum
        return Alignment == Alignment.Vertical ?
            heights.Sum() + (Items.Count - 1) * ItemSpacing :
            heights.Max();
    }

    protected override void SetTabInactiveCore()
    {
        foreach (TItem item in Items)
            item?.SetTabInactiveInternal();
    }

    private void OnSelectedItemChanged()
    {
        SelectedItemChanged?.Invoke(this, EventArgs.Empty);
    }

    #region Selection methods

    private int GetDimension(Component component, Rectangle contentRect)
    {
        return Alignment == Alignment.Vertical
            ? component.GetHeight((int)contentRect.Width, (int)contentRect.Height)
            : component.GetWidth((int)contentRect.Width, (int)contentRect.Height);
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
            ? (int)contentRect.Height
            : (int)contentRect.Width;
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
            Hexa.NET.ImGui.ImGui.SetScrollY(scroll);
        else
            Hexa.NET.ImGui.ImGui.SetScrollX(scroll);
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