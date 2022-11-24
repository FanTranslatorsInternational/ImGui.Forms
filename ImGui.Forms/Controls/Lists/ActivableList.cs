using System;
using System.Linq;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;

namespace ImGui.Forms.Controls.Lists
{
    public class ActivableList : BaseList<ActivableComponent>
    {
        public ActivableList()
        {
            InitializeItems();
        }

        #region Item initialization

        private void InitializeItems()
        {
            var items = new ObservableList<ActivableComponent>();

            items.ItemInserted += Items_NewItem;
            items.ItemAdded += Items_NewItem;
            items.ItemSet += Items_ItemSet;
            items.ItemRemoved += Items_ItemRemoved;

            Items = items;
        }

        private void Items_NewItem(object sender, ItemEventArgs<ActivableComponent> e)
        {
            e.Item.Activated += Item_Activated;
        }

        private void Items_ItemSet(object sender, ItemSetEventArgs<ActivableComponent> e)
        {
            e.PreviousItem.Activated -= Item_Activated;
            e.Item.Activated += Item_Activated;
        }

        private void Items_ItemRemoved(object sender, ItemEventArgs<ActivableComponent> e)
        {
            e.Item.Activated -= Item_Activated;
        }

        #endregion

        private void Item_Activated(object sender, EventArgs e)
        {
            // Otherwise, reset activation of all items, except the current one
            var senderItem = (ActivableComponent)sender;
            foreach (var item in Items.ToArray())
                item.Active = item == senderItem;
        }
    }
}
