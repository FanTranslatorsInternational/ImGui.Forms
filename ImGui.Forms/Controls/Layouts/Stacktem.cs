using System;
using ImGui.Forms.Controls.Base;

namespace ImGui.Forms.Controls.Layouts
{
    public class StackItem
    {
        public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

        public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

        public bool HasBorder { get; set; }

        public Component Content { get; }

        public StackItem(Component component)
        {
            Content = component ?? throw new ArgumentNullException(nameof(component));
        }

        public static implicit operator StackItem(Component c) => new StackItem(c);
        public static implicit operator StackItem(TableCell tc) => new StackItem(tc.Content) { HorizontalAlignment = tc.HorizontalAlignment, VerticalAlignment = tc.VerticalAlignment };
    }

    public enum VerticalAlignment
    {
        Top,
        Center,
        Bottom
    }

    public enum HorizontalAlignment
    {
        Right,
        Center,
        Left
    }
}
