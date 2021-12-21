using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace ImGui.Forms.Components.Menu
{
    public class MenuBarMenu : MenuBarItem
    {
        public bool Enabled { get; set; } = true;

        public string Caption { get; set; } = string.Empty;

        public Vector2 Padding { get; set; } = new Vector2(8, 8);

        public IList<MenuBarItem> Items { get; } = new List<MenuBarItem>();

        public override int Height => GetHeight();

        protected override void UpdateInternal()
        {
            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4, 5));
            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Padding);

            if (ImGuiNET.ImGui.BeginMenu(Caption, Enabled))
            {
                foreach (var item in Items)
                    item.Update();

                ImGuiNET.ImGui.EndMenu();
            }

            ImGuiNET.ImGui.PopStyleVar(2);
        }

        private int GetHeight()
        {
            ApplyStyles();

            var textSize = ImGuiNET.ImGui.CalcTextSize(Caption ?? string.Empty);
            var height = (int)(textSize.Y + ImGuiNET.ImGui.GetStyle().FramePadding.Y);

            RemoveStyles();

            return height;
        }
    }
}
