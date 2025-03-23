using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImGui.Forms.Extensions
{
    public static class ColorExtensions
    {
        public static uint ToUInt32(this ThemedColor c)
        {
            return ((Color)c).ToUInt32();
        }

        public static uint ToUInt32(this Color c)
        {
            var pixel = c.ToPixel<Rgba32>();
            return (uint)((pixel.A << 24) | (pixel.B << 16) | (pixel.G << 8) | pixel.R);
        }

        public static Color ToColor(this uint value)
        {
            return Color.FromRgba((byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24));
        }

        public static Vector4 ToVector4(this Color c)
        {
            var pixel = c.ToPixel<Rgba32>();
            return new Vector4(pixel.R / 255f, pixel.G / 255f, pixel.B / 255f, pixel.A / 255f);
        }

        public static Color ToColor(this Vector4 value)
        {
            return Color.FromRgba((byte)(value.X * 255), (byte)(value.Y * 255), (byte)(value.Z * 255), (byte)(value.W * 255));
        }
    }
}
