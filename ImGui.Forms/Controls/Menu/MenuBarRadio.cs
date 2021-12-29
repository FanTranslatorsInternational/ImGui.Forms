using System;
using System.Collections.Generic;
using System.Linq;
using ImGui.Forms.Controls.Layouts;

namespace ImGui.Forms.Controls.Menu
{
    public class MenuBarRadio : MenuBarItem
    {
        private readonly MenuBarMenu _radioMenu;

        public string Caption
        {
            get => _radioMenu.Caption;
            set => _radioMenu.Caption = value;
        }

        public IList<MenuBarCheckBox> CheckItems { get; }

        public MenuBarCheckBox SelectedItem { get; private set; }

        #region Events

        public event EventHandler SelectedItemChanged;

        #endregion

        public MenuBarRadio()
        {
            var checkItems = new ObservableList<MenuBarCheckBox>();
            checkItems.ItemAdded += CheckItems_ItemAdded;
            checkItems.ItemRemoved += CheckItems_ItemRemoved;

            _radioMenu = new MenuBarMenu();
            CheckItems = checkItems;
        }

        public override int Height => GetHeight();

        protected override void UpdateInternal()
        {
            _radioMenu.Update();
        }

        private int GetHeight()
        {
            ApplyStyles();

            var textSize = ImGuiNET.ImGui.CalcTextSize(Caption ?? string.Empty);
            var height = (int)(textSize.Y + ImGuiNET.ImGui.GetStyle().FramePadding.Y);

            RemoveStyles();

            return height;
        }

        private void CheckItems_ItemAdded(object sender, ItemEventArgs<MenuBarCheckBox> e)
        {
            e.Item.CheckChanged += Item_CheckChanged;
            _radioMenu.Items.Add(e.Item);
        }

        private void CheckItems_ItemRemoved(object sender, ItemEventArgs<MenuBarCheckBox> e)
        {
            e.Item.CheckChanged -= Item_CheckChanged;
            _radioMenu.Items.Remove(e.Item);
        }

        private void Item_CheckChanged(object sender, EventArgs e)
        {
            foreach (var language in _radioMenu.Items.Cast<MenuBarCheckBox>())
            {
                language.CheckChanged -= Item_CheckChanged;
                language.Checked = language == sender;
                language.CheckChanged += Item_CheckChanged;
            }

            SelectedItem = (MenuBarCheckBox)sender;
            SelectedItemChanged?.Invoke(this, new EventArgs());
        }
    }
}
