using System.Drawing;

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
    }
}
