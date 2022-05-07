using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
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

            ImGuiNET.ImGui.Image((IntPtr)Image, new Vector2(Image.Width, Image.Height));
        }
    }
}
