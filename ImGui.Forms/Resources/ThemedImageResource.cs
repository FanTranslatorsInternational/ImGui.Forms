using System;
using System.Numerics;
using ImGui.Forms.Models;

namespace ImGui.Forms.Resources
{
    public class ThemedImageResource
    {
        private readonly ImageResource _lightImage;
        private readonly ImageResource _darkImage;

        /// <summary>
        /// The size of the <see cref="ThemedImageResource"/> as a <see cref="Vector2"/>.
        /// </summary>
        public Vector2 Size => new Vector2(GetImage().Width, GetImage().Height);

        /// <summary>
        /// The width of the <see cref="ThemedImageResource"/>.
        /// </summary>
        public int Width => GetImage().Width;

        /// <summary>
        /// The height of the <see cref="ThemedImageResource"/>.
        /// </summary>
        public int Height => GetImage().Height;

        public ThemedImageResource(ImageResource lightImage, ImageResource darkImage)
        {
            _lightImage = lightImage;
            _darkImage = darkImage;
        }

        public void Destroy()
        {
            _lightImage.Destroy();
            _darkImage.Destroy();
        }

        public static explicit operator IntPtr(ThemedImageResource ir) => (IntPtr)ir.GetImage();

        public static implicit operator ThemedImageResource(ImageResource ir) => new ThemedImageResource(ir, ir);

        private ImageResource GetImage()
        {
            switch (Style.Theme)
            {
                case Theme.Light:
                    return _lightImage;

                case Theme.Dark:
                    return _darkImage;

                default:
                    throw new InvalidOperationException($"Unknown theme {Style.Theme}.");
            }
        }
    }
}
