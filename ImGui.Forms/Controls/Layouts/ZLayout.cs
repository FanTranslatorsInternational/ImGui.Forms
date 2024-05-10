using System.Collections.Generic;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using Veldrid;

namespace ImGui.Forms.Controls.Layouts
{
    public class ZLayout : Component
    {
        public IList<Component> Items { get; } = new List<Component>();

        public Vector2 ItemSpacing { get; set; }

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            if (ImGuiNET.ImGui.BeginChild($"{Id}", new Vector2(contentRect.Width, contentRect.Height)))
            {
                var largestHeight = 0;

                var (x, y) = (0, 0);
                for (var i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    if (!(item?.Visible ?? false))
                        continue;

                    var itemWidth = item.GetWidth(contentRect.Width);
                    var itemHeight = item.GetHeight(contentRect.Height);

                    // Wrap positions
                    if (x + itemWidth + ItemSpacing.X >= contentRect.Width)
                    {
                        x = 0;
                        y += largestHeight + (int)ItemSpacing.Y;

                        largestHeight = 0;
                    }

                    if (itemHeight > largestHeight)
                        largestHeight = itemHeight;

                    if (itemWidth == 0 || itemHeight == 0)
                        continue;

                    var scrollY = -(int)ImGuiNET.ImGui.GetScrollY();

                    ImGuiNET.ImGui.SetCursorPosX(x);
                    ImGuiNET.ImGui.SetCursorPosY(y);

                    // Draw component container
                    if (ImGuiNET.ImGui.BeginChild($"{Id}-{i}", new Vector2(itemWidth, itemHeight)))
                        // Draw component
                        item.Update(new Rectangle(contentRect.X + x, contentRect.Y + y + scrollY, itemWidth, itemHeight));

                    ImGuiNET.ImGui.EndChild();

                    // Advance component position
                    x += itemWidth + (int)ItemSpacing.X;
                }
            }

            ImGuiNET.ImGui.EndChild();
        }

        protected override void SetTabInactiveCore()
        {
            foreach (Component item in Items)
                item?.SetTabInactiveInternal();
        }
    }
}
