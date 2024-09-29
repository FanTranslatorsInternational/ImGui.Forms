using System;
using System.Collections.Generic;
using System.Linq;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Localization;
using ImGui.Forms.Resources;

namespace ImGui.Forms.Controls.Menu
{
    public class MenuBarRadio : MenuBarItem
    {
        private readonly MenuBarMenu _radioMenu;

        #region Properties

        public LocalizedString Text
        {
            get => _radioMenu.Text;
            set => _radioMenu.Text = value;
        }

        public IList<MenuBarCheckBox> CheckItems { get; }

        public MenuBarCheckBox SelectedItem { get; private set; }

        public override bool Enabled
        {
            get => _radioMenu.Enabled;
            set => _radioMenu.Enabled = value;
        }

        #endregion

        #region Events

        public event EventHandler SelectedItemChanged;

        #endregion

        public MenuBarRadio(LocalizedString text)
        {
            var checkItems = new ObservableList<MenuBarCheckBox>();
            checkItems.ItemAdded += CheckItems_ItemAdded;
            checkItems.ItemRemoved += CheckItems_ItemRemoved;
            checkItems.ItemSet += CheckItems_ItemSet;
            checkItems.ItemInserted += CheckItems_ItemInserted;

            _radioMenu = new MenuBarMenu(text);
            CheckItems = checkItems;
        }

        public override int Height => GetHeight();

        protected override void UpdateInternal()
        {
            _radioMenu.Update();
        }

        protected override void UpdateEventsInternal()
        {
            _radioMenu.UpdateEvents();
        }

        private int GetHeight()
        {
            ApplyStyles();

            var textSize = TextMeasurer.MeasureText(Text);
            var height = (int)(textSize.Y + ImGuiNET.ImGui.GetStyle().FramePadding.Y);

            RemoveStyles();

            return height;
        }

        private void CheckItems_ItemAdded(object sender, ItemEventArgs<MenuBarCheckBox> e)
        {
            e.Item.CheckChanged -= Item_CheckChanged;
            e.Item.CheckChanged += Item_CheckChanged;

            _radioMenu.Items.Add(e.Item);
        }

        private void CheckItems_ItemRemoved(object sender, ItemEventArgs<MenuBarCheckBox> e)
        {
            e.Item.CheckChanged -= Item_CheckChanged;

            _radioMenu.Items.Remove(e.Item);
        }

        private void CheckItems_ItemInserted(object sender, ItemEventArgs<MenuBarCheckBox> e)
        {
            e.Item.CheckChanged -= Item_CheckChanged;
            e.Item.CheckChanged += Item_CheckChanged;

            _radioMenu.Items.Insert(e.Index, e.Item);
        }

        private void CheckItems_ItemSet(object sender, ItemEventArgs<MenuBarCheckBox> e)
        {
            e.Item.CheckChanged -= Item_CheckChanged;
            e.Item.CheckChanged += Item_CheckChanged;

            _radioMenu.Items[e.Index] = e.Item;
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
