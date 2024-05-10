using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class Panel : Component
    {
        public Size Size { get; set; } = Size.Parent;

        public Component Content { get; set; }

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
}
