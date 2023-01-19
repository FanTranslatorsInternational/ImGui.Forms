using System.Drawing;

namespace ImGui.Forms.Controls.Lists
{
    public class DataTableRow<TData>
    {
        public TData Data { get; }
        public bool CanSelect { get; }

        public Color TextColor { get; set; }

        public DataTableRow(TData data, bool canSelect = true)
        {
            Data = data;
            CanSelect = canSelect;
        }
    }
}
