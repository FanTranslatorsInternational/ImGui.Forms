using System;
using ImGui.Forms.Resources;

namespace ImGui.Forms.Controls.Menu
{
    public class MenuBarButton : MenuBarItem
    {
        public bool Enabled { get; set; } = true;

        public string Caption { get; set; } = string.Empty;

        public override int Height => GetHeight();

        #region Events

        public event EventHandler Clicked;

        #endregion

        protected override void UpdateInternal()
        {
            // Add menu button
            if (ImGuiNET.ImGui.MenuItem(Caption ?? string.Empty, Enabled))
                // Execute click event, if set
                Clicked?.Invoke(this, new EventArgs());
        }

        private int GetHeight()
        {
            ApplyStyles();

            var textSize = FontResource.MeasureText(Caption);
            var height = (int)(textSize.Y + ImGuiNET.ImGui.GetStyle().FramePadding.Y);

            RemoveStyles();

            return height;
        }
    }
}
