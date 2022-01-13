using System.Drawing;

namespace ImGui.Forms.Controls.Lists
{
    public class DataTableRow<TData>
    {
        public TData Data { get; }

        public Color TextColor { get; set; }

        public DataTableRow(TData data)
        {
            Data = data;
        }
    }
}
