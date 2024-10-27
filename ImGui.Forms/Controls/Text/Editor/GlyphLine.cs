using System.Collections.Generic;

namespace ImGui.Forms.Controls.Text.Editor
{
    class GlyphLine
    {
        public bool HasCarriageReturn { get; set; }
        public List<Glyph> Glyphs { get; } = new();

        public int Length => Glyphs.Count;

        public Glyph this[int i]
        {
            get => Glyphs[i];
            set => Glyphs[i] = value;
        }
    }
}
