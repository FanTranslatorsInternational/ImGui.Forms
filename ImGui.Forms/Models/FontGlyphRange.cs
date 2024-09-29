using System;

namespace ImGui.Forms.Models
{
    [Flags]
    public enum FontGlyphRange
    {
        Latin = 1 << 0,
        Cyrillic = 1 << 1,
        ChineseJapaneseKorean = 1 << 2,
        Greek = 1 << 3,
        Thai = 1 << 4,
        Vietnamese = 1 << 5,
        Symbols = 1 << 6,

        All = 0x7F
    }
}
