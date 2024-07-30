using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;

namespace ImGui.Forms.Controls
{
    public class TabPage
    {
        public LocalizedString Title { get; set; }

        public Component Content { get; }
        
        public bool HasChanges { get; set; }

        public TabPage(Component content)
        {
            Content = content;
        }
    }
}
