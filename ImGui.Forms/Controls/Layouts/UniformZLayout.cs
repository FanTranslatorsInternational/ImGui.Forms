using System;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace ImGui.Forms.Controls.Layouts
{
    public class UniformZLayout : Component
    {
        private readonly Vector2 _elementSize;

        private float _scrollY;
        private int _scrollToItemIndex;

        #region Properties

        public IList<Component> Items { get; } = [];

        public Vector2 ItemSpacing { get; set; }

        public Size Size { get; set; } = Size.Parent;

        #endregion

        public UniformZLayout(Vector2 elementSize)
        {
            _elementSize = elementSize;
        }

        public override Size GetSize() => Size;

        public void ScrollToItem(Component item)
        {
            int itemIndex = Items.IndexOf(item);
            if (itemIndex < 0)
                return;

            _scrollToItemIndex = itemIndex;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            if (ImGuiNET.ImGui.BeginChild($"{Id}", new Vector2(contentRect.Width, contentRect.Height)))
            {
                var columns = (int)Math.Floor(contentRect.Width / (_elementSize.X + ItemSpacing.X));
                if ((_elementSize.X + ItemSpacing.X) * columns + _elementSize.X <= contentRect.Width)
                    columns++;

                int rows = Items.Count / columns + (Items.Count % columns > 0 ? 1 : 0);

                float fixedScroll = -1f;
                if (_scrollToItemIndex >= 0)
                {
                    fixedScroll = _scrollToItemIndex / columns * (_elementSize.Y + ItemSpacing.Y);

                    _scrollToItemIndex = -1;
                }

                float currentScroll = UpdateScroll(fixedScroll);
                float height = rows * _elementSize.Y + (rows - 1) * ItemSpacing.Y;
                if (ImGuiNET.ImGui.BeginChild($"{Id}_inner", new Vector2(contentRect.Width, height)))
                    UpdateContent(contentRect, currentScroll, columns, rows);

                ImGuiNET.ImGui.EndChild();
            }

            ImGuiNET.ImGui.EndChild();
        }

        private void UpdateContent(Rectangle contentRect, float currentScroll, int columns, int rows)
        {
            var startRow = (int)Math.Floor(currentScroll / (_elementSize.Y + ItemSpacing.Y));
            var endRow = (int)Math.Ceiling((contentRect.Height + currentScroll) / (_elementSize.Y + ItemSpacing.Y));

            for (int row = startRow; row < endRow; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    int itemIndex = row * columns + column;
                    if (itemIndex >= Items.Count)
                        break;

                    Component item = Items[itemIndex];
                    if (!(item?.Visible ?? false))
                        continue;

                    var x = column * (_elementSize.X + ItemSpacing.X);
                    var y = row * (_elementSize.Y + ItemSpacing.Y);

                    ImGuiNET.ImGui.SetCursorPosX(x);
                    ImGuiNET.ImGui.SetCursorPosY(y);

                    // Draw component container
                    if (ImGuiNET.ImGui.BeginChild($"{Id}_inner-{itemIndex}", _elementSize))
                        // Draw component
                        item.Update(new Rectangle((int)(contentRect.X + x), (int)(contentRect.Y + y - currentScroll), (int)_elementSize.X, (int)_elementSize.Y));

                    ImGuiNET.ImGui.EndChild();
                }
            }
        }

        private float UpdateScroll(float fixedScroll)
        {
            if (fixedScroll >= 0)
            {
                ImGuiNET.ImGui.SetScrollY(fixedScroll);

                return fixedScroll;
            }

            float newScrollY = ImGuiNET.ImGui.GetScrollY();

            if (_scrollY == newScrollY)
                return _scrollY;

            if (IsTabInactiveCore())
                ImGuiNET.ImGui.SetScrollY(_scrollY);

            return _scrollY = newScrollY;
        }
    }
}
