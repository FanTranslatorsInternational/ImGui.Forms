using ImGui.Forms.Localization;
using ImGui.Forms.Resources;

namespace ImGui.Forms.Controls.Lists
{
    public class ImageListItem
    {
        private ImageResource _baseImg;

        public ImageResource Image
        {
            get => _baseImg;
            set
            {
                _baseImg?.Destroy();
                _baseImg = value;
            }
        }

        public LocalizedString Text { get; set; }
    }
}
