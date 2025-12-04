using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using Veldrid;

namespace ImGui.Forms.Controls;

public class Panel : Component
{
    #region Properties

    public Component Content { get; set; }

    public Size Size { get; set; } = Size.Parent;

    #endregion

    public Panel(Component content = default)
    {
        Content = content;
    }

    public override Size GetSize()
    {
        return Size;
    }

    protected override void UpdateInternal(Rectangle contentRect)
    {
        Content?.Update(contentRect);
    }

    protected override void SetTabInactiveCore()
    {
        Content?.SetTabInactiveInternal();
    }
}