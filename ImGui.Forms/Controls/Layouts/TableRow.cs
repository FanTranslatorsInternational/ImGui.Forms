using System.Collections.Generic;

namespace ImGui.Forms.Controls.Layouts
{
    public class TableRow
    {
        internal TableLayout _parent;

        private readonly ObservableList<TableCell> _cells = new ObservableList<TableCell>();

        public IList<TableCell> Cells => _cells;

        public TableRow()
        {
            _cells.ItemAdded += _cells_ItemAdded;
            _cells.ItemRemoved += _cells_ItemRemoved;
            _cells.ItemSet += _cells_ItemSet;
            _cells.ItemInserted += _cells_ItemInserted;
        }

        private void _cells_ItemAdded(object sender, ItemEventArgs<TableCell> e)
        {
            _parent?.Cells_ItemAdded();
        }

        private void _cells_ItemRemoved(object sender, ItemEventArgs<TableCell> e)
        {
            _parent?.Cells_ItemRemoved();
        }

        private void _cells_ItemInserted(object sender, ItemEventArgs<TableCell> e)
        {
            _parent?.Cells_ItemInserted();
        }

        private void _cells_ItemSet(object sender, ItemEventArgs<TableCell> e)
        {
            _parent?.Cells_ItemSet();
        }
    }
}
