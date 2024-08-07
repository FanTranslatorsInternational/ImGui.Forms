﻿using System;
using ImGui.Forms.Localization;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;

namespace ImGui.Forms.Controls.Menu
{
    public class MenuBarButton : MenuBarItem
    {
        public LocalizedString Text { get; set; } = string.Empty;

        public KeyCommand KeyAction { get; set; }

        public override int Height => GetHeight();

        #region Events

        public event EventHandler Clicked;

        #endregion

        protected override void UpdateInternal()
        {
            // Add menu button
            if (ImGuiNET.ImGui.MenuItem(Text, Enabled) || IsKeyDown(KeyAction))
                // Execute click event, if set
                Clicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void UpdateEventsInternal()
        {
            // Add menu button
            if (IsKeyDown(KeyAction))
                // Execute click event, if set
                Clicked?.Invoke(this, EventArgs.Empty);
        }

        private int GetHeight()
        {
            ApplyStyles();

            var textSize = FontResource.MeasureText(Text);
            var height = (int)(textSize.Y + ImGuiNET.ImGui.GetStyle().FramePadding.Y);

            RemoveStyles();

            return height;
        }
    }
}
