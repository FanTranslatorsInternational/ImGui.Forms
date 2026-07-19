using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Support;

namespace ImGui.Forms.Controls;

public class Panel : Component
{
    #region Properties

    public Component? Content { get; set; }

    public Size Size { get; set; } = Size.Parent;

    #endregion

    public Panel(Component? content = null)
    {
        Content = content;
    }

    public override Size GetSize()
    {
        return Size;
    }

    protected override void UpdateInternal(Rectangle contentRect)
    {
        Hexa.NET.ImGui.ImGui.SetCursorScreenPos(contentRect.Position);

        if (Hexa.NET.ImGui.ImGui.BeginChild($"{Id}-panel", contentRect.Size))
            Content?.Update(contentRect);

        Hexa.NET.ImGui.ImGui.EndChild();
    }
}