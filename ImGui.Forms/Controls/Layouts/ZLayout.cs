using System.Collections.Generic;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Support;

namespace ImGui.Forms.Controls.Layouts;

public class ZLayout : Component
{
    #region Properties

    public IList<Component> Items { get; } = new List<Component>();

    public Vector2 ItemSpacing { get; set; }

    public Size Size { get; set; } = Size.Parent;

    #endregion

    public override Size GetSize()
    {
        return Size;
    }

    protected override void UpdateInternal(Rectangle contentRect)
    {
        if (Hexa.NET.ImGui.ImGui.BeginChild($"{Id}", new Vector2(contentRect.Width, contentRect.Height)))
        {
            var largestHeight = 0;

            var (x, y) = (0, 0);
            for (var i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                if (!(item?.Visible ?? false))
                    continue;

                var itemWidth = item.GetWidth((int)contentRect.Width, (int)contentRect.Height);
                var itemHeight = item.GetHeight((int)contentRect.Width, (int)contentRect.Height);

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

                var scrollY = -(int)Hexa.NET.ImGui.ImGui.GetScrollY();

                Hexa.NET.ImGui.ImGui.SetCursorPosX(x);
                Hexa.NET.ImGui.ImGui.SetCursorPosY(y);

                // Draw component container
                if (Hexa.NET.ImGui.ImGui.BeginChild($"{Id}-{i}", new Vector2(itemWidth, itemHeight)))
                    // Draw component
                    item.Update(new Rectangle(new Vector2(contentRect.X + x, contentRect.Y + y + scrollY), new Vector2(itemWidth, itemHeight)));

                Hexa.NET.ImGui.ImGui.EndChild();

                // Advance component position
                x += itemWidth + (int)ItemSpacing.X;
            }
        }

        Hexa.NET.ImGui.ImGui.EndChild();
    }

    protected override void SetTabInactiveCore()
    {
        foreach (Component item in Items)
            item?.SetTabInactiveInternal();
    }
}