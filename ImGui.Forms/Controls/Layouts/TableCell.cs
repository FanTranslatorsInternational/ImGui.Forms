using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;

namespace ImGui.Forms.Controls.Layouts
{
    public class TableCell
    {
        private Size _sizeValue;
        internal bool HasSize;

        /// <summary>
        /// The content of the cell.
        /// </summary>
        public Component Content { get; }

        /// <summary>
        /// The vertical alignment of the content inside the cell.
        /// </summary>
        public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

        /// <summary>
        /// The horizontal alignment of the content inside the cell.
        /// </summary>
        public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

        /// <summary>
        /// The size of this <see cref="TableCell"/>.
        /// Is set to <see cref="Models.Size"/> of <see cref="Content"/> by default.
        /// </summary>
        public Size Size
        {
            get => HasSize ? _sizeValue : Content?.GetSize() ?? Size.Parent;
            set
            {
                _sizeValue = value;
                HasSize = true;
            }
        }

        /// <summary>
        /// Determines if the component should have a visible border.
        /// </summary>
        public bool ShowBorder { get; set; }

        public TableCell(Component component)
        {
            Content = component;
        }

        public int GetWidth(int parentWidth, float layoutCorrection = 1f)
        {
            if (Size.Width.IsContentAligned)
                return Content?.GetWidth(parentWidth, layoutCorrection) ?? 0;

            return Component.GetDimension(Size.Width, parentWidth, layoutCorrection);
        }

        public int GetHeight(int parentHeight, float layoutCorrection = 1f)
        {
            if (Size.Height.IsContentAligned)
                return Content?.GetHeight(parentHeight, layoutCorrection) ?? 0;

            return Component.GetDimension(Size.Height, parentHeight, layoutCorrection);
        }

        public static implicit operator TableCell(Component c) => new TableCell(c);
    }
}
