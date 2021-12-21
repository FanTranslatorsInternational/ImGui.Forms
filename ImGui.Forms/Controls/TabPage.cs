using ImGui.Forms.Controls.Base;

namespace ImGui.Forms.Controls
{
    public class TabPage
    {
        public string Title { get; set; } = string.Empty;

        public Component Content { get; }

        public bool HasChanges { get; set; } = false;

        public TabPage(Component content)
        {
            Content = content;
        }
    }
}
