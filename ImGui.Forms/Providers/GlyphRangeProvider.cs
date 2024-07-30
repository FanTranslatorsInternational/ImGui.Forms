using System.Text;

namespace ImGui.Forms.Providers
{
    internal class GlyphRangeProvider
    {
        private static StringBuilder _sb = new();

        private static readonly int[] _latinRange = new[]
        {
            0x0020,0x024f
        };

        private static readonly int[] _cyrillicRange = new[]
        {
            0x0400,0x04ff,
            0x0500,0x052f,
            0x1c80,0x1c8f,
            0x1d2b,0x1d2b,
            0x1d78,0x1d78,
            0x2de0,0x2dff,
            0xa640,0xa69f,
            0xfe2e,0xfe2f,
            0x1e030,0x1e08f
        };

        private static readonly int[] _cjkRange = new[]
        {
            0x2e80,0x2fd5,
            0x3000,0x303f,
            0x3041,0x3096,
            0x30a0,0x30ff,
            0x31f0,0x31ff,
            0x3220,0x3243,
            0x3280,0x337f,
            0x3400,0x4db5,
            0x4e00,0x9fff,
            0xf900,0xfa6a,
            0xff01,0xff9f
        };

        private static readonly int[] _greekRange = new[]
        {
            0x0370,0x03ff,
            0x1d26,0x1d2a,
            0x1d5d,0x1d61,
            0x1d66,0x1d6a,
            0x1dbf,0x1dbf,
            0x1f00,0x1fff,
            0x2126,0x2126,
            0xab65,0xab65,
            0x10140,0x1018f,
            0x101a0,0x101a0,
            0x1d200,0x1d245
        };

        private static readonly int[] _thaiRange = new[]
        {
            0x0e00,0x0e7f
        };

        private static readonly int[] _symbolRange = new[]
        {
            0x2000,0x206f,
            0x2150,0x218f,
            0x2600,0x26ff
        };

        public static string GetLatinRange()
        {
            return GetRangeText(_latinRange);
        }

        public static string GetCyrillicRange()
        {
            return GetRangeText(_cyrillicRange);
        }

        public static string GetCjkRange()
        {
            return GetRangeText(_cjkRange);
        }

        public static string GetGreekRange()
        {
            return GetRangeText(_greekRange);
        }

        public static string GetThaiRange()
        {
            return GetRangeText(_thaiRange);
        }

        public static string GetSymbolRange()
        {
            return GetRangeText(_symbolRange);
        }

        private static string GetRangeText(int[] range)
        {
            _sb.Clear();

            for (var i = 0; i < range.Length; i += 2)
                for (var j = range[i]; j <= range[i + 1]; j++)
                    _sb.Append((char)j);

            return _sb.ToString();
        }
    }
}
