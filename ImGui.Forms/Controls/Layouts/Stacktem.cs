using ImGui.Forms.Controls.Base;

namespace ImGui.Forms.Controls.Layouts;

public class StackItem : TableCell
{
    public StackItem(Component component) : base(component) { }

    public static implicit operator StackItem(Component c) => new(c);
}

public enum VerticalAlignment
{
    Top,
    Center,
    Bottom
}

public enum HorizontalAlignment
{
    Left,
    Center,
    Right
}