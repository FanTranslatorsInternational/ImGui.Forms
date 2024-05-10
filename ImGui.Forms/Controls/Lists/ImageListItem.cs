using ImGui.Forms.Localization;
using ImGui.Forms.Resources;

namespace ImGui.Forms.Controls.Lists
{
    public class ImageListItem
    {
        private ThemedImageResource _baseImg;

        public ThemedImageResource Image
        {
            get => _baseImg;
            set
            {
                _baseImg?.Destroy();
                _baseImg = value;
            }
        }

        public bool RetainAspectRatio { get; set; } = true;

        public LocalizedString Text { get; set; }
    }
}
