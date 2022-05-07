using System;
using ImGui.Forms.Resources;

namespace ImGui.Forms.Controls.Menu
{
    public class MenuBarCheckBox : MenuBarItem
    {
        private bool _checked;

        public bool Enabled { get; set; } = true;

        public string Caption { get; set; } = string.Empty;

        public override int Height => GetHeight();

        public bool Checked
        {
            get => _checked;
            set
            {
                _checked = value;
                OnCheckedChanged();
            }
        }

        #region Events

        public event EventHandler CheckChanged;

        #endregion

        protected override void UpdateInternal()
        {
            // Add menu check box
            if (ImGuiNET.ImGui.MenuItem(Caption ?? string.Empty, null, Checked, Enabled))
                // Invert checked value, if clicked
                Checked = !Checked;
        }

        private int GetHeight()
        {
            ApplyStyles();

            var textSize = FontResource.MeasureText(Caption);
            var height = (int)(textSize.Y + ImGuiNET.ImGui.GetStyle().FramePadding.Y);

            RemoveStyles();

            return height;
        }

        private void OnCheckedChanged()
        {
            CheckChanged?.Invoke(this, new EventArgs());
        }
    }
}
