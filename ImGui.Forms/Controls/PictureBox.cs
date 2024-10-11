using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class PictureBox : Component
    {
        private ThemedImageResource _baseImg;

        #region Properties

        public Size Size { get; set; } = Size.Content;

        public ThemedImageResource Image
        {
            get => _baseImg;
            set
            {
                _baseImg?.Destroy();
                _baseImg = value;
            }
        }

        #endregion

        public PictureBox(ThemedImageResource image = default)
        {
            Image = image;
        }

        public override Size GetSize()
        {
            SizeValue width = Size.Width.IsContentAligned
                ? SizeValue.Absolute(_baseImg?.Width ?? 0)
                : Size.Width;

            SizeValue height = Size.Height.IsContentAligned
                ? SizeValue.Absolute(_baseImg?.Height ?? 0)
                : Size.Height;

            return new Size(width, height);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            if (_baseImg == null || (nint)_baseImg == nint.Zero)
                return;

            ImGuiNET.ImGui.Image((nint)Image, contentRect.Size);
        }
    }
}
