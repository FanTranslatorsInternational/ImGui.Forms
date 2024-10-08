﻿using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;

namespace ImGui.Forms.Controls
{
    public class TabPage
    {
        #region Properties

        public LocalizedString Title { get; set; }

        public Component Content { get; }
        
        public bool HasChanges { get; set; }

        #endregion

        public TabPage(Component content)
        {
            Content = content;
        }
    }
}
