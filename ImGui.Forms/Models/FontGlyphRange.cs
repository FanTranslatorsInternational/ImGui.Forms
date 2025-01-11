using System;

namespace ImGui.Forms.Models
{
    [Flags]
    public enum FontGlyphRange
    {
        Latin = 1 << 0,
        Cyrillic = 1 << 1,
        ChineseJapanese = 1 << 2,
        Korean = 1 << 3,
        Greek = 1 << 4,
        Thai = 1 << 5,
        Vietnamese = 1 << 6,
        Symbols = 1 << 7,

        All = 0xFF
    }
}
