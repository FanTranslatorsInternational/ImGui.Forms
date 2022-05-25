using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using Veldrid;

namespace ImGui.Forms.Controls.Lists
{
    public class List : Component
    {
        public IList<Component> Items { get; set; } = new List<Component>();

        public int ItemSpacing { get; set; }

        public bool HasBorder { get; set; }

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var localItems = Items?.ToArray() ?? Array.Empty<Component>();
            var listHeight = localItems.Sum(i => i.GetHeight(contentRect.Height)) + Math.Max(0, localItems.Length - 1) * ItemSpacing;

            if (ImGuiNET.ImGui.BeginChild($"{Id}", new Vector2(contentRect.Width, contentRect.Height), HasBorder))
            {
                var scrollbarWidth = listHeight > contentRect.Height ? ImGuiNET.ImGui.GetStyle().ScrollbarSize : 0;
                var scrollY = -(int)ImGuiNET.ImGui.GetScrollY();
                var y = 0;

                if (ImGuiNET.ImGui.BeginChild($"{Id}_in", new Vector2(contentRect.Width, listHeight)))
                {
                    for (var i = 0; i < localItems.Length; i++)
                    {
                        var itemHeight = localItems[i].GetHeight(contentRect.Height);

                        ImGuiNET.ImGui.SetCursorPos(new Vector2(0, y));
                        if (ImGuiNET.ImGui.BeginChild($"{Id}_itm{i}", new Vector2(contentRect.Width - (int)scrollbarWidth, itemHeight)))
                            localItems[i].Update(new Rectangle(contentRect.X, contentRect.Y + y + scrollY, contentRect.Width - (int)scrollbarWidth, itemHeight));

                        ImGuiNET.ImGui.EndChild();

                        y += itemHeight + ItemSpacing;
                    }
                }

                ImGuiNET.ImGui.EndChild();
            }

            ImGuiNET.ImGui.EndChild();
        }
    }
}
