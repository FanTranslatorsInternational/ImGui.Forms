using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ImGui.Forms.Providers;

internal class GlyphRangeProvider
{
    private static readonly StringBuilder Sb = new();

    private static readonly ushort[] LatinRange = new ushort[]
    {
        0x0020,0x024f
    };

    private static readonly ushort[] CyrillicRange = new ushort[]
    {
        0x0400,0x04ff,
        0x0500,0x052f,
        0x1c80,0x1c8f,
        0x1d2b,0x1d2b,
        0x1d78,0x1d78,
        0x2de0,0x2dff,
        0xa640,0xa69f,
        0xfe2e,0xfe2f,
        //0x1e030,0x1e08f
    };

    private static readonly ushort[] CjRange = new ushort[]
    {
        0x2026,0x2026,
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

    private static readonly ushort[] KoreanRange = new ushort[]
    {
        0x3131, 0x3163,
        0xac00, 0xd79d
    };

    private static readonly ushort[] GreekRange = new ushort[]
    {
        0x0370,0x03ff,
        0x1d26,0x1d2a,
        0x1d5d,0x1d61,
        0x1d66,0x1d6a,
        0x1dbf,0x1dbf,
        0x1f00,0x1fff,
        0x2126,0x2126,
        0xab65,0xab65,
        //0x10140,0x1018f,
        //0x101a0,0x101a0,
        //0x1d200,0x1d245
    };

    private static readonly ushort[] ThaiRange = new ushort[]
    {
        0x0e00,0x0e7f
    };

    private static readonly ushort[] VietnameseRange = new ushort[]
    {
        0x0020, 0x00FF,
        0x0102, 0x0103,
        0x0110, 0x0111,
        0x0128, 0x0129,
        0x0168, 0x0169,
        0x01A0, 0x01A1,
        0x01AF, 0x01B0,
        0x1EA0, 0x1EF9
    };

    private static readonly ushort[] SymbolRange = new ushort[]
    {
        0x2000,0x206f,
        0x2150,0x218f,
        0x2600,0x26ff
    };

    public static nint GetLatinRange()
    {
        return GetPointer(LatinRange);
    }

    public static nint GetCyrillicRange()
    {
        return GetPointer(CyrillicRange);
    }

    public static nint GetCjRange()
    {
        return GetPointer(CjRange);
    }

    public static nint GetKoreanRange()
    {
        return GetPointer(KoreanRange);
    }

    public static nint GetGreekRange()
    {
        return GetPointer(GreekRange);
    }

    public static nint GetThaiRange()
    {
        return GetPointer(ThaiRange);
    }

    public static nint GetVietnameseRange()
    {
        return GetPointer(VietnameseRange);
    }

    public static nint GetSymbolRange()
    {
        return GetPointer(SymbolRange);
    }

    private static nint GetPointer(ushort[] ranges)
    {
        var newRanges = new ushort[ranges.Length + 1];
        Array.Copy(ranges, newRanges, ranges.Length);

        var handle = GCHandle.Alloc(newRanges, GCHandleType.Pinned);
        return handle.AddrOfPinnedObject();
    }
}