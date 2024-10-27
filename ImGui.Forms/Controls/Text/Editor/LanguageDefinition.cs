using System.Collections.Generic;

namespace ImGui.Forms.Controls.Text.Editor
{
    public class LanguageDefinition
    {
        public string Name { get; protected set; } = string.Empty;

        public IReadOnlySet<string> Keywords { get; protected set; } = new HashSet<string>();
        public IReadOnlyDictionary<string, Identifier> Identifiers { get; protected set; } = new Dictionary<string, Identifier>();
        public IReadOnlyDictionary<string, Identifier> Preprocessors { get; protected set; } = new Dictionary<string, Identifier>();
        public IReadOnlyList<(string, PaletteIndex)> TokenRegularExpressions { get; protected set; } = new List<(string, PaletteIndex)>();

        public string SingleLineCommentIdentifier { get; protected set; } = string.Empty;
        public string CommentStartIdentifier { get; protected set; } = string.Empty;
        public string CommentEndIdentifier { get; protected set; } = string.Empty;
        public char PreprocessorCharacter { get; protected set; } = '#';

        public bool IsAutoIndentation { get; protected set; }
        public bool IsCaseSensitive { get; protected set; }


        public virtual bool CanTokenize() => false;

        public virtual bool Tokenize(string text, int inBegin, int inEnd,
            out int outBegin, out int outEnd, out PaletteIndex paletteIndex)
        {
            outBegin = 0;
            outEnd = 0;
            paletteIndex = 0;

            return false;
        }
    }

}
