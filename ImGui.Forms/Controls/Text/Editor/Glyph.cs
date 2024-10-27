namespace ImGui.Forms.Controls.Text.Editor
{
    class Glyph
    {
        public char Character { get; }
        public PaletteIndex ColorIndex { get; set; }

        public bool IsComment { get; set; }
        public bool IsMultiLineComment { get; set; }
        public bool IsPreprocessor { get; set; }

        public Glyph(char character, PaletteIndex colorIndex = PaletteIndex.Default)
        {
            Character = character;
            ColorIndex = colorIndex;
        }
    }
}
