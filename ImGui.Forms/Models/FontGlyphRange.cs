using System;

namespace ImGui.Forms.Models
{
    [Flags]
    public enum FontGlyphRange
    {
        Default = 1 << 0,
        Cyrillic = 1 << 1,
        Chinese = 1 << 2,
        Japanese = 1 << 3,
        Greek = 1 << 4,
        Korean = 1 << 5,
        Thai = 1 << 6,
        Vietnamese = 1 << 7,

        All = 255,
    }
}
