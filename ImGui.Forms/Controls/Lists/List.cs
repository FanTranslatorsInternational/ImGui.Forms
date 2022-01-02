using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls.Lists
{
    public class List : Component
    {
        public IList<Component> Items { get; } = new List<Component>();

        public int ItemSpacing { get; set; }

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var localItems = Items.ToArray();
            var listHeight = localItems.Sum(i => i.GetHeight(contentRect.Height)) + Math.Max(0, Items.Count - 1) * ItemSpacing;

            // Render list container
            if (ImGuiNET.ImGui.BeginChild($"{Id}", new Vector2(contentRect.Width, contentRect.Height)))
            {
                // Render inner item container
                var scrollbarWidth = contentRect.Height <= listHeight ? ImGuiNET.ImGui.GetStyle().ScrollbarSize : 0;
                if (ImGuiNET.ImGui.BeginChild($"{Id}_inner", new Vector2(contentRect.Width - scrollbarWidth, listHeight)))
                {
                    var y = 0;
                    for (var i = 0; i < localItems.Length; i++)
                    {
                        var item = localItems[i];
                        var itemHeight = item.GetHeight(contentRect.Height);

                        // Render item container
                        if (ImGuiNET.ImGui.BeginChild($"##{Id}_item{i}", new Vector2(contentRect.Width - scrollbarWidth, itemHeight)))
                        {
                            // Render item
                            item.Update(new Rectangle(contentRect.X, contentRect.Y + y, contentRect.Width - (int)scrollbarWidth, itemHeight));

                            ImGuiNET.ImGui.EndChild();
                        }

                        y += itemHeight + ItemSpacing;
                    }

                    ImGuiNET.ImGui.EndChild();
                }

                ImGuiNET.ImGui.EndChild();
            }
        }
    }
}
