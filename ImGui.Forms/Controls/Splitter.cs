using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class Splitter : Component
    {
        public Alignment Alignment { get; set; } = Alignment.Vertical;

        public SizeValue Length { get; set; }

        public override Size GetSize() => Alignment == Alignment.Horizontal ? new Size(Length, 1) : new Size(1, Length);

        protected override void UpdateInternal(Rectangle contentRect)
        {
            ImGuiNET.ImGui.GetWindowDrawList().AddRectFilled(contentRect.Position, contentRect.Position + contentRect.Size, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Border));
        }
    }
}
