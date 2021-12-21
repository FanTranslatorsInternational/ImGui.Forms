using ImGui.Forms.Controls.Base;

namespace ImGui.Forms.Controls.Layouts
{
    public class TableCell
    {
        public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

        public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

        public Component Content { get; }

        public TableCell(Component component)
        {
            Content = component;
        }

        public static implicit operator TableCell(Component c) => new TableCell(c);
        public static implicit operator TableCell(StackItem si) => new TableCell(si.Content) { VerticalAlignment = si.VerticalAlignment, HorizontalAlignment = si.HorizontalAlignment };
    }
}
