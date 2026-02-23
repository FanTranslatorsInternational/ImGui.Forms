using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Models;
using ImGui.Forms.Support;

namespace ImGui.Forms.Controls;

public class Splitter : Component
{
    #region Properties

    public Alignment Alignment { get; set; }

    public SizeValue Length { get; set; } = SizeValue.Parent;

    #endregion

    public Splitter(Alignment alignment = default)
    {
        Alignment = alignment;
    }

    public override Size GetSize() => Alignment == Alignment.Horizontal ? new Size(Length, 1) : new Size(1, Length);

    protected override void UpdateInternal(Rectangle contentRect)
    {
        Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRectFilled(contentRect.Position, contentRect.Position + contentRect.Size, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.Border));
    }
}