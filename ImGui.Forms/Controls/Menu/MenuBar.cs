﻿using System.Collections.Generic;
using System.Linq;
using ImGui.Forms.Resources;
using ImGuiNET;

namespace ImGui.Forms.Controls.Menu
{
    public class MenuBar
    {
        private readonly bool _isMain;

        public IList<MenuBarItem> Items { get; } = new List<MenuBarItem>();

        public FontResource Font { get; set; }

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
            if (Font != null)
                ImGuiNET.ImGui.PushFont((ImFontPtr)Font);

            // Begin menu bar
            bool isMenuOpen = _isMain ? 
                ImGuiNET.ImGui.BeginMainMenuBar() : 
                ImGuiNET.ImGui.BeginMenuBar();
            if (isMenuOpen)
            {
                // Add content to menu bar
                foreach (var child in Items)
                    child.Update();

                if (_isMain) ImGuiNET.ImGui.EndMainMenuBar();
                else ImGuiNET.ImGui.EndMenuBar();
            }
            else
            {
                foreach (var child in Items)
                    child.UpdateEvents();
            }

            if (Font != null)
                ImGuiNET.ImGui.PopFont();
        }

        private int GetHeight()
        {
            if (Font != null)
                ImGuiNET.ImGui.PushFont((ImFontPtr)Font);

            // HINT: It's currently unknown where those 3 pixels come from, but they have to be added to get the correct size of the menu
            var height = Items.Max(x => x.Height) + 3;

            if (Font != null)
                ImGuiNET.ImGui.PopFont();

            return height;
        }
    }
}
