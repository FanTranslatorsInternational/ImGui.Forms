using System.Drawing;

namespace ImGui.Forms.Extensions
{
    static class ColorExtensions
    {
        public static uint ToUInt32(this Color c)
        {
            return (uint)((c.A << 24) | (c.B << 16) | (c.G << 8) | c.R);
        }
    }
}
