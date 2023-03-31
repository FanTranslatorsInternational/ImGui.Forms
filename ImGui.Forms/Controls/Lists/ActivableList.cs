using System;
using System.Linq;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;

namespace ImGui.Forms.Controls.Lists
{
    public class ActivableList : BaseList<ActivableComponent>
    {
        protected override void OnItemAdded(ItemEventArgs<ActivableComponent> e)
        {
            e.Item.Activated += Item_Activated;
        }

        protected override void OnItemSet(ItemSetEventArgs<ActivableComponent> e)
        {
            e.PreviousItem.Activated -= Item_Activated;
            e.Item.Activated += Item_Activated;
        }

        protected override void OnItemRemoved(ItemEventArgs<ActivableComponent> e)
        {
            e.Item.Activated -= Item_Activated;
        }

        private void Item_Activated(object sender, EventArgs e)
        {
            // Otherwise, reset activation of all items, except the current one
            var senderItem = (ActivableComponent)sender;
            foreach (var item in Items.ToArray())
                item.Active = item == senderItem;
        }
    }
}
