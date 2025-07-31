using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class ZoomablePictureBox : ZoomableComponent
    {
        private ThemedImageResource? _baseImg;

        #region Properties

        public ThemedImageResource? Image
        {
            get => _baseImg;
            set
            {
                _baseImg?.Destroy();
                _baseImg = value;
            }
        }

        public bool ShowImageBorder { get; set; }

        public ThemedColor BackgroundColor { get; set; }

        #endregion

        public ZoomablePictureBox(ThemedImageResource? image = null)
        {
            Image = image;
        }

        protected override void DrawInternal(Rectangle contentRect)
        {
            // Draw background color
            if (!BackgroundColor.IsEmpty)
                ImGuiNET.ImGui.GetWindowDrawList().AddRectFilled(contentRect.Position, contentRect.Position + contentRect.Size, BackgroundColor.ToUInt32());

            // Draw image
            if (!HasValidImage())
                return;

            Rectangle imageRect = GetTransformedImageRect(contentRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddImage((nint)_baseImg!, imageRect.Position, imageRect.Position + imageRect.Size);

            if (ShowImageBorder)
                ImGuiNET.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Style.GetColor(ImGuiCol.Border).ToUInt32());
        }

        protected bool HasValidImage()
        {
            return _baseImg != null && (nint)_baseImg != nint.Zero;
        }

        protected Rectangle GetTransformedImageRect(Rectangle contentRect)
        {
            var imageStartPosition = -(_baseImg!.Size / 2);
            var imageRect = new Rectangle((int)imageStartPosition.X, (int)imageStartPosition.Y, _baseImg.Width, _baseImg.Height);
            return Transform(contentRect, imageRect);
        }
    }
}
