using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class PictureBox : Component
    {
        private ImageResource _img;

        public ImageResource Image
        {
            get => _img;
            set
            {
                _img?.Destroy();
                _img = value;
            }
        }

        public override Size GetSize()
        {
            return new Size(Image.Width, Image.Height);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            if (Image == null || (IntPtr)_img == IntPtr.Zero)
                return;

            //contentRect = new Rectangle((int) ImGui.GetCursorScreenPos().X, (int) ImGui.GetCursorScreenPos().Y, Image.Width, Image.Height);
            //ImGui.GetWindowDrawList().AddImage((IntPtr)_img, new Vector2(contentRect.X, contentRect.Y), new Vector2(contentRect.X + contentRect.Width, contentRect.Y + contentRect.Height));
            ImGuiNET.ImGui.Image((IntPtr)Image, new Vector2(Image.Width, Image.Height));
        }
    }
}
