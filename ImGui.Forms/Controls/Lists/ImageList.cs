using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Extensions;
using ImGui.Forms.Factories;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGui.Forms.Support;
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

        public Vector2 ThumbnailSize { get; set; } = new(30, 30);
        public bool ShowThumbnailBorder { get; set; }

        public FontResource Font { get; set; }

        public Vector2 Padding { get; set; }
        public int ItemPadding { get; set; }

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
            ImageListItem selectedItem = null;

            if (ImGuiNET.ImGui.BeginChild($"##{Id}_out", new Vector2(contentRect.Width, contentRect.Height), ImGuiChildFlags.None))
            {
                var textHeight = TextMeasurer.GetCurrentLineHeight(Font);

                var itemHeight = Math.Max((int)ThumbnailSize.Y, textHeight);
                var itemDimensions = new Vector2(contentRect.Width - Padding.X * 2, itemHeight);
                var contentPos = new Vector2(contentRect.X + Padding.X, contentRect.Y + Padding.Y);
                var scrollY = ImGuiNET.ImGui.GetScrollY();

                var localItems = Items.ToArray();

                if (ImGuiNET.ImGui.BeginChild($"##{Id}_in", new Vector2(contentRect.Width, Padding.Y * 2 + localItems.Length * (itemHeight + ItemPadding)), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar))
                {
                    for (var i = 0; i < localItems.Length; i++)
                    {
                        var item = localItems[i];
                        var itemId = IdFactory.Get(item);

                        var itemPos = new Vector2(Padding.X, i * itemHeight + Padding.Y + i * ItemPadding);
                        var contentItemPos = contentPos + itemPos;
                        var contentScrollPos = contentItemPos - new Vector2(0, scrollY);
                        var contentScrollEndPos = contentScrollPos + itemDimensions;

                        // Create dummy control for states
                        ImGuiNET.ImGui.PushID(itemId);
                        ImGuiSupport.Dummy(itemId, itemPos - new Vector2(0, scrollY), itemDimensions);

                        // Create item states
                        var isItemSelected = item == SelectedItem;
                        var isItemHovered = ImGuiNET.ImGui.IsItemHovered() &&
                                            (int)(ImGuiNET.ImGui.GetMousePos().Y - contentPos.Y - Padding.Y + scrollY) /
                                            (itemHeight + ItemPadding) == i;
                        var isItemClicked = isItemHovered && ImGuiNET.ImGui.IsMouseDown(ImGuiMouseButton.Left);

                        ImGuiNET.ImGui.PopID();

                        // Set selected item locally
                        if (isItemHovered && ImGuiNET.ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                            selectedItem = item;

                        // Add background color for selection
                        var color = isItemClicked ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.HeaderActive) :
                        isItemHovered ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.HeaderHovered) :
                        isItemSelected ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.Header) : 0;

                        if (isItemHovered || isItemSelected)
                            ImGuiNET.ImGui.GetWindowDrawList().AddRectFilled(contentScrollPos, contentScrollEndPos, color);

                        // Add thumbnail
                        if (item.Image != null && ThumbnailSize.X != 0 && ThumbnailSize.Y != 0)
                        {
                            var imgPos = contentScrollPos;
                            var thumbnailRect = new Vector2(ThumbnailSize.X, ThumbnailSize.Y);

                            if (item.RetainAspectRatio)
                            {
                                var heightSmaller = item.Image.Height < item.Image.Width;
                                var ratio = heightSmaller ?
                                    (float)item.Image.Height / item.Image.Width :
                                    (float)item.Image.Width / item.Image.Height;

                                var retainedWidth = !heightSmaller ? ThumbnailSize.Y * ratio : ThumbnailSize.X;
                                var retainedHeight = heightSmaller ? ThumbnailSize.X * ratio : ThumbnailSize.Y;

                                imgPos = heightSmaller ?
                                    new Vector2(imgPos.X, imgPos.Y + (ThumbnailSize.Y - retainedHeight) / 2f) :
                                    new Vector2(imgPos.X + (ThumbnailSize.X - retainedWidth) / 2f, imgPos.Y);
                                thumbnailRect = new Vector2(retainedWidth, retainedHeight);
                            }

                            ImGuiNET.ImGui.GetWindowDrawList().AddImage((nint)item.Image, imgPos, imgPos + thumbnailRect);

                            if (ShowThumbnailBorder)
                                ImGuiNET.ImGui.GetWindowDrawList().AddRect(contentScrollPos, contentScrollPos + new Vector2(ThumbnailSize.X, ThumbnailSize.Y), Style.GetColor(ImGuiCol.Border).ToUInt32(), 0);
                        }

                        // Add text
                        var textPos = contentScrollPos + new Vector2(ThumbnailSize.X + 2, (itemHeight - textHeight) / 2f);

                        ImFontPtr? fontPtr = Font?.GetPointer();
                        if (fontPtr != null)
                            ImGuiNET.ImGui.GetWindowDrawList().AddText(fontPtr.Value, Font.Data.Size, textPos, 0xFFFFFFFF, item.Text);
                        else
                            ImGuiNET.ImGui.GetWindowDrawList().AddText(textPos, 0xFFFFFFFF, item.Text);
                    }
                }

                ImGuiNET.ImGui.EndChild();
            }

            ImGuiNET.ImGui.EndChild();

            // Invoke selected item change event
            if (selectedItem != null && SelectedItem != selectedItem)
            {
                SelectedItem = selectedItem;
                OnSelectedItemChanged();
            }
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
