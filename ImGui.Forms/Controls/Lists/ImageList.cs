using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls.Lists
{
    public class ImageList : Component
    {
        // HINT: ImGuiCol.Header is used for selected items
        // HINT: ImGuiCol.HeaderHovered is used for hovered items
        // HINT: ImGuiCol.HeaderActive is used for hovered and clicked items

        public IList<ImageListItem> Items { get; }
        public ImageListItem SelectedItem { get; set; }
        public Size ThumbnailSize { get; set; } = new Size(30, 30);

        public FontResource Font { get; set; }

        public Vector2 Padding { get; set; } = new Vector2(2, 2);
        public int ItemPadding { get; set; } = 2;

        public Size Size { get; set; } = Size.Parent;

        #region Events

        public event EventHandler SelectedItemChanged;

        #endregion

        public ImageList()
        {
            var items = new ObservableList<ImageListItem>();
            items.ItemAdded += Items_ItemAdded;
            items.ItemRemoved += Items_ItemRemoved;
            items.ItemSet += Items_ItemSet;
            items.ItemInserted += Items_ItemInserted;

            Items = items;
        }

        public override Size GetSize() => Size;

        protected override void UpdateInternal(Rectangle contentRect)
        {
            if (ImGuiNET.ImGui.BeginChild($"##{Id}", new Vector2(contentRect.Width, contentRect.Height), false))
            {
                var textHeight = FontResource.GetCurrentLineHeight(Font);

                var itemHeight = Math.Max((int)ThumbnailSize.Height.Value, textHeight);
                var itemDimensions = new Vector2(contentRect.Width - Padding.X * 2, itemHeight);
                var contentPos = new Vector2(contentRect.X + Padding.X, contentRect.Y + Padding.Y);
                var thumbnailRect = new Vector2(ThumbnailSize.Width.Value, ThumbnailSize.Height.Value);
                var scrollY = ImGuiNET.ImGui.GetScrollY();

                var localItems = Items.ToArray();

                if (ImGuiNET.ImGui.BeginChild($"##{Id}_in", new Vector2(contentRect.Width, Padding.Y * 2 + localItems.Length * (itemHeight + ItemPadding)), false, ImGuiWindowFlags.NoScrollbar))
                {
                    if (IsHovering(contentRect) && ImGuiNET.ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        var selected = SelectedItem;
                        var selectedIndex = (int)(ImGuiNET.ImGui.GetMousePos().Y - contentPos.Y - Padding.Y + scrollY) / (itemHeight + ItemPadding);
                        if (selectedIndex >= 0 && selectedIndex < localItems.Length)
                            SelectedItem = localItems[selectedIndex];

                        if (selected != SelectedItem)
                            OnSelectedItemChanged();
                    }

                    for (var i = 0; i < localItems.Length; i++)
                    {
                        var item = localItems[i];

                        var isItemSelected = item == SelectedItem;
                        var isItemHovered = IsHovering(contentRect) &&
                                            (int)(ImGuiNET.ImGui.GetMousePos().Y - contentPos.Y - Padding.Y + scrollY) /
                                            (itemHeight + ItemPadding) == i;
                        var isItemClicked = isItemHovered && ImGuiNET.ImGui.IsMouseDown(ImGuiMouseButton.Left);

                        var itemPos = new Vector2(Padding.X, i * itemHeight + Padding.Y + i * ItemPadding);
                        var contentItemPos = contentPos + itemPos;
                        var contentScrollPos = contentItemPos - new Vector2(0, scrollY);
                        var contentScrollEndPos = contentScrollPos + itemDimensions;

                        // Add background color for selection
                        var color = isItemClicked ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.HeaderActive) :
                            isItemHovered ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.HeaderHovered) :
                            isItemSelected ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.Header) : 0;

                        if (isItemHovered || isItemSelected)
                            ImGuiNET.ImGui.GetWindowDrawList().AddRectFilled(contentScrollPos, contentScrollEndPos, color);

                        // Add thumbnail
                        if (item.Image != null)
                        {
                            var imgPos = contentScrollPos + new Vector2();
                            ImGuiNET.ImGui.GetWindowDrawList().AddImage((IntPtr)item.Image, contentScrollPos, contentScrollPos + thumbnailRect);
                        }

                        // Add text
                        var textPos = contentScrollPos + new Vector2(ThumbnailSize.Width.Value + 2, (itemHeight - textHeight) / 2f);

                        if (Font != null)
                            ImGuiNET.ImGui.GetWindowDrawList().AddText((ImFontPtr)Font, Font.Size, textPos, 0xFFFFFFFF, item.Text);
                        else
                            ImGuiNET.ImGui.GetWindowDrawList().AddText(textPos, 0xFFFFFFFF, item.Text);
                    }
                }

                ImGuiNET.ImGui.EndChild();
            }

            ImGuiNET.ImGui.EndChild();
        }

        private bool IsHovering(Rectangle contentRect)
        {
            return ImGuiNET.ImGui.IsMouseHoveringRect(new Vector2(contentRect.X, contentRect.Y),
                new Vector2(contentRect.X + contentRect.Width, contentRect.Y + contentRect.Height));
        }

        #region Event Invokers

        private void OnSelectedItemChanged()
        {
            SelectedItemChanged?.Invoke(this, new EventArgs());
        }

        #endregion

        #region Event Methods

        private void Items_ItemAdded(object sender, ItemEventArgs<ImageListItem> e)
        {
        }

        private void Items_ItemRemoved(object sender, ItemEventArgs<ImageListItem> e)
        {
        }

        private void Items_ItemInserted(object sender, ItemEventArgs<ImageListItem> e)
        {
        }

        private void Items_ItemSet(object sender, ItemEventArgs<ImageListItem> e)
        {
        }

        #endregion
    }
}
