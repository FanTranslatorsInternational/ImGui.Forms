using System.Collections.Generic;
using System.Linq;
using ImGui.Forms.Models;
using ImGuiNET;

namespace ImGui.Forms.Controls.Menu
{
    public class MenuBar
    {
        private readonly bool _isMain;

        public IList<MenuBarItem> Items { get; } = new List<MenuBarItem>();

        public FontResource FontResource { get; set; }

        public int Height => GetHeight();

        protected MenuBar() : this(false)
        {
        }

        protected MenuBar(bool isMain)
        {
            _isMain = isMain;
        }

        public void Update()
        {
            if (FontResource != null)
                ImGuiNET.ImGui.PushFont((ImFontPtr)FontResource);

            // Begin menu bar
            if (_isMain) ImGuiNET.ImGui.BeginMainMenuBar();
            else ImGuiNET.ImGui.BeginMenuBar();

            // Add content to menu bar
            foreach (var child in Items)
                child.Update();

            // End menu bar
            if (_isMain) ImGuiNET.ImGui.EndMainMenuBar();
            else ImGuiNET.ImGui.EndMenuBar();

            if (FontResource != null)
                ImGuiNET.ImGui.PopFont();
        }

        private int GetHeight()
        {
            if (FontResource != null)
                ImGuiNET.ImGui.PushFont((ImFontPtr)FontResource);

            // HINT: It's currently unknown where those 3 pixels come from, but they have to be added to get the correct size of the menu
            var height = Items.Max(x => x.Height) + 3;

            if (FontResource != null)
                ImGuiNET.ImGui.PopFont();

            return height;
        }
    }
}
