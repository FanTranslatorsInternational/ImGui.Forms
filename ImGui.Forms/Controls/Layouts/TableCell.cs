using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;

namespace ImGui.Forms.Controls.Layouts
{
    public class TableCell
    {
        private Size _sizeValue;

        public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

        public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

        public bool HasBorder { get; set; }

        public Component Content { get; }

        /// <summary>
        /// The size of this <see cref="TableCell"/>.
        /// Is set to <see cref="Models.Size"/> of <see cref="Content"/> by default.
        /// </summary>
        public Size Size
        {
            get => _sizeValue ?? Content?.GetSize() ?? Size.Parent;
            set => _sizeValue = value;
        }

        public TableCell(Component component)
        {
            Content = component;
        }

        public int GetWidth(int parentWidth, float layoutCorrection = 1f)
        {
            return Component.GetDimension(Size.Width, parentWidth, layoutCorrection);
        }

        public int GetHeight(int parentHeight, float layoutCorrection = 1f)
        {
            return Component.GetDimension(Size.Height, parentHeight, layoutCorrection);
        }

        public static implicit operator TableCell(Component c) => new TableCell(c);
    }
}
