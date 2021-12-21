using System;
using System.Drawing;

namespace ImGui.Forms.Models
{
    public class ImageResource
    {
        private readonly Bitmap _img;
        private IntPtr _ptr = IntPtr.Zero;

        public int Width => _img.Width;

        public int Height => _img.Height;

        public ImageResource(Bitmap image)
        {
            _img = image;
        }

        public void Destroy()
        {
            if (_ptr != IntPtr.Zero)
                Application.Instance?.ImageFactory.UnloadImage(_ptr);
        }

        public static implicit operator ImageResource(Bitmap i) => new ImageResource(i);
        public static explicit operator IntPtr(ImageResource ir) => ir.GetPointer();

        private IntPtr GetPointer()
        {
            if (_ptr != IntPtr.Zero)
                return _ptr;

            return _ptr = Application.Instance?.ImageFactory.LoadImage(_img) ?? IntPtr.Zero;
        }
    }
}
