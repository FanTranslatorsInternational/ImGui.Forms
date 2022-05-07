using System.Drawing;
using System.Numerics;

namespace ImGui.Forms.Extensions
{
    public static class ColorExtensions
    {
        public static uint ToUInt32(this Color c)
        {
            return (uint)((c.A << 24) | (c.B << 16) | (c.G << 8) | c.R);
        }

        public static Color ToColor(this uint value)
        {
            return Color.FromArgb((byte)(value >> 24), (byte)value, (byte)(value >> 8), (byte)(value >> 16));
        }

        public static Vector4 ToVector4(this Color c)
        {
            return new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
        }

        public static Color ToColor(this Vector4 value)
        {
            return Color.FromArgb((int)(value.W * 255), (int)(value.X * 255), (int)(value.Y * 255), (int)(value.Z * 255));
        }
    }
}
