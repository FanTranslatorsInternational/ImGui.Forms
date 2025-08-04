using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using Veldrid;

namespace ImGui.Forms.Controls.Text.Editor
{
    // Original implementation in C++ and for Dear ImGui by BalazsJako: https://github.com/BalazsJako/ImGuiColorTextEdit
    // Modified by onepiecefreak:
    // - Translated to C#
    // - Adjusted to ImGui.Forms paradigms and code design
    // - Word finding and cursor movements was changed to match Notepad++ more instead

    public class TextEditor : Component
    {
        private object _lock = new();

        private int _tabSize = 4;

        private bool _withinRender;
        private bool _scrollToCursor;
        private bool _scrollToSelection;
        private bool _scrollToTop;
        private bool _isTextChanged;
        private bool _isCursorPositionChanged;
        private float _lastClick = -1f;
        private float _textStart = 20f;
        private double _startTime = (DateTime.Now - DateTime.UnixEpoch).TotalMilliseconds;
        private SelectionMode _selectionMode;

        private List<GlyphLine> _lines = new();
        private string _lineBuffer = string.Empty;
        private readonly List<(Regex, PaletteIndex)> _languageRegexList = new();

        private int _colorRangeMin;
        private int _colorRangeMax;
        private bool _isCheckComments = true;
        private LanguageDefinition _languageDefinition = new();
        private uint[] _setPalette = new uint[(int)PaletteIndex.Max];
        private readonly uint[] _currentPalette = new uint[(int)PaletteIndex.Max];

        private int _undoIndex;
        private readonly List<UndoRecord> _undoBuffer = new();

        private Dictionary<int, string> _errorMarkers = new();
        private HashSet<int> _breakpoints = new();

        internal EditorState mState;

        private Vector2 _characterAdvance;

        private Coordinate _interactiveStart;
        private Coordinate _interactiveEnd;

        #region Properties

        public int TabSize
        {
            get => _tabSize;
            set => _tabSize = Math.Clamp(value, 0, 32);
        }

        public float LineSpacing { get; set; } = 1f;
        public float LeftMargin { get; set; } = 10f;

        public bool IsReadOnly { get; set; }
        public bool IsOverwrite { get; private set; }
        public bool IsColorizerEnabled { get; set; } = true;
        public bool IsShowingLineNumbers { get; set; } = true;
        public bool IsShowingWhitespaces { get; set; }
        public bool IsHandleMouseInputsEnabled { get; set; } = true;
        public bool IsHandleKeyboardInputsEnabled { get; set; } = true;

        #endregion

        #region Events

        public event EventHandler<string> TextChanged;
        public event EventHandler<Coordinate> CursorPositionChanged;

        #endregion

        public TextEditor()
        {
            SetPalette(GetDarkPalette());
            _lines.Add(new GlyphLine());
        }

        public void SetLanguageDefinition(LanguageDefinition aLanguageDef)
        {
            _languageDefinition = aLanguageDef;
            _languageRegexList.Clear();

            foreach ((string first, PaletteIndex second) in _languageDefinition.TokenRegularExpressions)
                _languageRegexList.Add((new Regex(first, RegexOptions.Compiled), second));

            Colorize();
        }

        public LanguageDefinition GetLanguageDefinition() => _languageDefinition;

        public void SetPalette(uint[] aValue)
        {
            _setPalette = aValue;
        }

        public uint[] GetPalette() => _setPalette;

        public void SetErrorMarkers(Dictionary<int, string> aMarkers) => _errorMarkers = aMarkers;

        public void SetBreakpoints(HashSet<int> aMarkers) => _breakpoints = aMarkers;

        public string GetText(Coordinate aCoordinate)
        {
            Coordinate endCoordinate = AdvanceCoordinate(aCoordinate, 1);

            return GetText(aCoordinate, endCoordinate);
        }

        public string GetText(Coordinate aStart, Coordinate aEnd)
        {
            lock (_lock)
            {
                int lstart = aStart.Line;
                int lend = aEnd.Line;
                int istart = GetCharacterIndex(aStart);
                int iend = GetCharacterIndex(aEnd);
                var s = 0;

                for (int i = lstart; i < lend; i++)
                    s += _lines[i].Length;

                StringBuilder result = new(s + s / 8);

                while (istart < iend || lstart < lend)
                {
                    if (lstart >= _lines.Count)
                        break;

                    GlyphLine line = _lines[lstart];
                    if (istart < line.Length)
                    {
                        result.Append(line[istart].Character);
                        istart++;
                    }
                    else
                    {
                        istart = 0;
                        ++lstart;

                        if (istart >= iend && (lstart >= lend || lstart >= _lines.Count))
                            break;

                        if (line.HasCarriageReturn)
                            result.Append('\r');
                        result.Append('\n');
                    }
                }

                return result.ToString();
            }
        }

        private Coordinate GetActualCursorCoordinates()
        {
            return SanitizeCoordinates(mState.CursorPosition);
        }

        public Coordinate GetCursorPosition() => GetActualCursorCoordinates();

        private Coordinate SanitizeCoordinates(Coordinate aValue)
        {
            int line = aValue.Line;
            int column = aValue.Column;
            if (line >= _lines.Count)
            {
                if (_lines.Count <= 0)
                {
                    line = 0;
                    column = 0;
                }
                else
                {
                    line = _lines.Count - 1;
                    column = GetLineMaxColumn(line);
                }

                return new Coordinate(line, column);
            }
            else
            {
                column = _lines.Count <= 0 ? 0 : Math.Min(column, GetLineMaxColumn(line));
                return new Coordinate(line, column);
            }
        }

        public Coordinate AdvanceCoordinate(Coordinate aCoord, int aCount)
        {
            for (var i = 0; i < aCount; i++)
                Advance(ref aCoord);

            return aCoord;
        }

        private void Advance(ref Coordinate aCoordinate)
        {
            if (aCoordinate.Line < _lines.Count)
            {
                GlyphLine line = _lines[aCoordinate.Line];
                int cindex = GetCharacterIndex(aCoordinate);

                if (cindex + 1 < line.Length)
                {
                    cindex++;
                }
                else
                {
                    aCoordinate.Line++;
                    cindex = 0;
                }

                aCoordinate.Column = GetCharacterColumn(aCoordinate.Line, cindex);
            }
        }

        internal void DeleteRange(Coordinate aStart, Coordinate aEnd)
        {
            if (aEnd <= aStart || IsReadOnly)
                return;

            int start = GetCharacterIndex(aStart);
            int end = GetCharacterIndex(aEnd);

            if (aStart.Line == aEnd.Line)
            {
                GlyphLine line = _lines[aStart.Line];
                int n = GetLineMaxColumn(aStart.Line);
                if (aEnd.Column >= n)
                    line.Glyphs.RemoveRange(start, line.Length - start);
                else
                    line.Glyphs.RemoveRange(start, end - start);
            }
            else
            {
                GlyphLine firstLine = _lines[aStart.Line];
                GlyphLine lastLine = _lines[aEnd.Line];

                firstLine.Glyphs.RemoveRange(start, firstLine.Length - start);
                lastLine.Glyphs.RemoveRange(0, end);

                if (aStart.Line < aEnd.Line)
                    firstLine.Glyphs.AddRange(lastLine.Glyphs);

                if (aStart.Line < aEnd.Line)
                    RemoveLine(aStart.Line + 1, aEnd.Line + 1);
            }

            _isTextChanged = true;
        }

        internal int InsertTextAt(ref Coordinate aWhere, string aValue)
        {
            if (IsReadOnly)
                return 0;

            int cindex = GetCharacterIndex(aWhere);
            var totalLines = 0;
            foreach (char character in aValue)
            {
                if (_lines.Count <= 0)
                    break;

                switch (character)
                {
                    case '\r':
                        // skip
                        continue;
                    case '\n':
                        {
                            if (cindex < _lines[aWhere.Line].Length)
                            {
                                GlyphLine newLine = InsertLine(aWhere.Line + 1);
                                GlyphLine line = _lines[aWhere.Line];
                                newLine?.Glyphs.InsertRange(0, line.Glyphs[cindex..]);
                                line.Glyphs.RemoveRange(cindex, line.Length - cindex);
                            }
                            else
                            {
                                InsertLine(aWhere.Line + 1);
                            }

                            ++aWhere.Line;
                            aWhere.Column = 0;
                            cindex = 0;
                            ++totalLines;
                            break;
                        }
                    default:
                        {
                            GlyphLine line = _lines[aWhere.Line];
                            line.Glyphs.Insert(cindex++, new Glyph(character));
                            ++aWhere.Column;
                            break;
                        }
                }

                _isTextChanged = true;
            }

            return totalLines;
        }

        private void AddUndo(UndoRecord aValue)
        {
            if (IsReadOnly)
                return;

            _undoBuffer.RemoveRange(_undoIndex, _undoBuffer.Count - _undoIndex);

            _undoBuffer.Add(aValue);
            ++_undoIndex;
        }

        private Coordinate ScreenPosToCoordinates(Vector2 aPosition)
        {
            Vector2 origin = ImGuiNET.ImGui.GetCursorScreenPos();
            Vector2 local = new(aPosition.X - origin.X, aPosition.Y - origin.Y);

            int lineNo = Math.Max(0, (int)Math.Floor(local.Y / _characterAdvance.Y));

            var columnCoord = 0;

            if (lineNo < _lines.Count)
            {
                GlyphLine line = _lines[lineNo];

                var columnIndex = 0;
                var columnX = 0.0f;

                while (columnIndex < line.Length)
                {
                    var columnWidth = 0.0f;

                    if (line[columnIndex].Character == '\t')
                    {
                        float spaceSize = ImGuiNET.ImGui.GetFont().CalcTextSizeA(ImGuiNET.ImGui.GetFontSize(), float.MaxValue, -1.0f, " ").X;
                        float oldX = columnX;
                        float newColumnX = (float)(1.0f + Math.Floor((1.0f + columnX) / (TabSize * spaceSize))) * (TabSize * spaceSize);
                        columnWidth = newColumnX - oldX;
                        if (_textStart + columnX + columnWidth * 0.5f > local.X)
                            break;
                        columnX = newColumnX;
                        columnCoord = columnCoord / TabSize * TabSize + TabSize;
                        columnIndex++;
                    }
                    else
                    {
                        columnWidth = ImGuiNET.ImGui.GetFont().CalcTextSizeA(ImGuiNET.ImGui.GetFontSize(), float.MaxValue, -1.0f, $"{line[columnIndex++].Character}").X;
                        if (_textStart + columnX + columnWidth * 0.5f > local.X)
                            break;

                        columnX += columnWidth;
                        columnCoord++;
                    }
                }
            }

            return SanitizeCoordinates(new Coordinate(lineNo, columnCoord));
        }

        private Coordinate FindWordStart(Coordinate aFrom)
        {
            Coordinate at = aFrom;
            if (at.Line >= _lines.Count)
                return at;

            GlyphLine line = _lines[at.Line];
            int cindex = GetCharacterIndex(at);

            if (cindex >= line.Length)
                return at;

            char startChar = line[cindex].Character;
            PaletteIndex cstart = line[cindex].ColorIndex;

            bool letterOrDigit = char.IsLetterOrDigit(startChar);

            while (cindex > 0)
            {
                if (cstart != line[cindex - 1].ColorIndex)
                    break;

                if (letterOrDigit)
                {
                    if (!char.IsLetterOrDigit(line[cindex - 1].Character))
                        break;
                }
                else
                {
                    if (startChar != line[cindex - 1].Character)
                        break;
                }

                --cindex;
            }

            return new Coordinate(at.Line, GetCharacterColumn(at.Line, cindex));
        }

        private Coordinate FindWordEnd(Coordinate aFrom)
        {
            Coordinate at = aFrom;
            if (at.Line >= _lines.Count)
                return at;

            GlyphLine line = _lines[at.Line];
            int cindex = GetCharacterIndex(at);

            if (cindex >= line.Length)
                return at;

            PaletteIndex cstart = line[cindex].ColorIndex;
            while (cindex < line.Length)
            {
                if (cstart != line[cindex].ColorIndex)
                    break;

                char startChar = line[cindex].Character;
                bool letterOrDigit = char.IsLetterOrDigit(startChar);

                while (cindex < line.Length)
                {
                    if (letterOrDigit)
                    {
                        if (!char.IsLetterOrDigit(line[cindex].Character))
                            break;
                    }
                    else
                    {
                        if (startChar != line[cindex].Character)
                            break;
                    }

                    ++cindex;
                }
                break;
            }

            return new Coordinate(aFrom.Line, GetCharacterColumn(aFrom.Line, cindex));
        }

        private Coordinate FindNextWord(Coordinate aFrom)
        {
            Coordinate at = aFrom;
            if (at.Line >= _lines.Count)
                return at;

            // skip to the next non-word character
            int cindex = GetCharacterIndex(aFrom);
            var isword = false;
            var skip = false;
            if (cindex < _lines[at.Line].Length)
            {
                GlyphLine line = _lines[at.Line];
                isword = char.IsAsciiLetterOrDigit(line[cindex].Character);
                skip = isword;
            }

            while (!isword || skip)
            {
                if (at.Line >= _lines.Count)
                {
                    int l = Math.Max(0, _lines.Count - 1);
                    return new Coordinate(l, GetLineMaxColumn(l));
                }

                GlyphLine line = _lines[at.Line];
                if (cindex < line.Length)
                {
                    isword = char.IsAsciiLetterOrDigit(line[cindex].Character);

                    switch (isword)
                    {
                        case true when !skip:
                            return new Coordinate(at.Line, GetCharacterColumn(at.Line, cindex));
                        case false:
                            skip = false;
                            break;
                    }

                    cindex++;
                }
                else
                {
                    cindex = 0;
                    ++at.Line;
                    skip = false;
                    isword = false;
                }
            }

            return at;
        }

        private int GetCharacterIndex(Coordinate aCoordinate)
        {
            if (aCoordinate.Line >= _lines.Count)
                return -1;

            GlyphLine line = _lines[aCoordinate.Line];
            var c = 0;
            var i = 0;
            for (; i < line.Length && c < aCoordinate.Column; i++)
            {
                if (line[i].Character == '\t')
                    c = c / TabSize * TabSize + TabSize;
                else
                    ++c;
            }

            return i;
        }

        public Coordinate GetCharacterCoordinates(int aPosition)
        {
            var line = 0;
            var pos = 0;

            int charCount;
            while ((charCount = GetLineCharacterCount(line) + (_lines[line].HasCarriageReturn ? 2 : 1)) + pos < aPosition)
            {
                if (line >= _lines.Count)
                    break;

                pos += charCount;
                line++;
            }

            return new Coordinate(line, GetCharacterColumn(line, aPosition - pos));
        }

        private int GetCharacterColumn(int aLine, int aIndex)
        {
            if (aLine >= _lines.Count)
                return 0;

            GlyphLine line = _lines[aLine];
            var col = 0;
            var i = 0;
            while (i < aIndex && i < line.Length)
            {
                char c = line[i].Character;
                i++;

                if (c == '\t')
                    col = col / TabSize * TabSize + TabSize;
                else
                    col++;
            }

            return col;
        }

        private int GetLineCharacterCount(int aLine)
        {
            return aLine >= _lines.Count ? 0 : _lines[aLine].Length;
        }

        private int GetLineMaxColumn(int aLine)
        {
            if (aLine >= _lines.Count)
                return 0;

            GlyphLine line = _lines[aLine];
            var col = 0;
            for (var i = 0; i < line.Length; i++)
            {
                char c = line[i].Character;
                if (c == '\t')
                    col = col / TabSize * TabSize + TabSize;
                else
                    col++;
            }

            return col;
        }

        private bool IsOnWordBoundary(Coordinate aAt)
        {
            if (aAt.Line >= _lines.Count || aAt.Column == 0)
                return true;

            GlyphLine line = _lines[aAt.Line];
            int cindex = GetCharacterIndex(aAt);
            if (cindex >= line.Length)
                return true;

            if (IsColorizerEnabled)
                return line[cindex].ColorIndex != line[cindex - 1].ColorIndex;

            return char.IsWhiteSpace(line[cindex].Character) != char.IsWhiteSpace(line[cindex - 1].Character);
        }

        private void RemoveLine(int aStart, int aEnd)
        {
            if (IsReadOnly || aEnd < aStart || _lines.Count <= aEnd - aStart)
                return;

            Dictionary<int, string> etmp = new();
            foreach (KeyValuePair<int, string> errorMarker in _errorMarkers)
            {
                int key = errorMarker.Key >= aStart ? errorMarker.Key - 1 : errorMarker.Key;
                if (key >= aStart && key <= aEnd)
                    continue;

                etmp[key] = errorMarker.Value;
            }

            _errorMarkers = etmp;

            HashSet<int> btmp = new();
            foreach (int breakpoint in _breakpoints)
            {
                if (breakpoint >= aStart && breakpoint <= aEnd)
                    continue;

                btmp.Add(breakpoint >= aStart ? breakpoint - 1 : breakpoint);
            }

            _breakpoints = btmp;

            _lines.RemoveRange(aStart, aEnd - aStart);
            if (_lines.Count <= 0)
                throw new InvalidOperationException("No more lines left.");

            _isTextChanged = true;
        }

        private void RemoveLine(int aIndex)
        {
            if (IsReadOnly || _lines.Count <= 1)
                return;

            Dictionary<int, string> etmp = new();
            foreach (KeyValuePair<int, string> errorMarker in _errorMarkers)
            {
                int key = errorMarker.Key > aIndex ? errorMarker.Key - 1 : errorMarker.Key;
                if (key - 1 == aIndex)
                    continue;

                etmp[key] = errorMarker.Value;
            }

            _errorMarkers = etmp;

            HashSet<int> btmp = new();
            foreach (int breakpoint in _breakpoints)
            {
                if (breakpoint == aIndex)
                    continue;

                btmp.Add(breakpoint >= aIndex ? breakpoint - 1 : breakpoint);
            }

            _breakpoints = btmp;

            _lines.RemoveAt(aIndex);
            if (_lines.Count <= 0)
                throw new InvalidOperationException("No more lines left.");

            _isTextChanged = true;
        }

        private GlyphLine InsertLine(int aIndex)
        {
            if (IsReadOnly)
                return null;

            var result = new GlyphLine();
            _lines.Insert(aIndex, result);

            Dictionary<int, string> etmp = new();
            foreach (KeyValuePair<int, string> errorMarker in _errorMarkers)
                etmp[errorMarker.Key >= aIndex ? errorMarker.Key + 1 : errorMarker.Key] = errorMarker.Value;

            _errorMarkers = etmp;

            HashSet<int> btmp = new();
            foreach (int breakpoint in _breakpoints)
                btmp.Add(breakpoint >= aIndex ? breakpoint + 1 : breakpoint);

            _breakpoints = btmp;

            return result;
        }

        private string GetWordUnderCursor()
        {
            Coordinate c = GetCursorPosition();
            return GetWordAt(c);
        }

        private string GetWordAt(Coordinate aCoords)
        {
            Coordinate start = FindWordStart(aCoords);
            Coordinate end = FindWordEnd(aCoords);

            var r = string.Empty;

            int istart = GetCharacterIndex(start);
            int iend = GetCharacterIndex(end);

            for (int it = istart; it < iend; ++it)
                r += _lines[aCoords.Line][it].Character;

            return r;
        }

        private uint GetGlyphColor(Glyph aGlyph)
        {
            if (!IsColorizerEnabled)
                return _currentPalette[(int)PaletteIndex.Default];
            if (aGlyph.IsComment)
                return _currentPalette[(int)PaletteIndex.Comment];
            if (aGlyph.IsMultiLineComment)
                return _currentPalette[(int)PaletteIndex.MultiLineComment];
            uint color = _currentPalette[(int)aGlyph.ColorIndex];
            if (aGlyph.IsPreprocessor)
            {
                uint ppcolor = _currentPalette[(int)PaletteIndex.Preprocessor];
                uint c0 = ((ppcolor & 0xff) + (color & 0xff)) / 2;
                uint c1 = ((ppcolor >> 8 & 0xff) + (color >> 8 & 0xff)) / 2;
                uint c2 = ((ppcolor >> 16 & 0xff) + (color >> 16 & 0xff)) / 2;
                uint c3 = ((ppcolor >> 24 & 0xff) + (color >> 24 & 0xff)) / 2;
                return c0 | c1 << 8 | c2 << 16 | c3 << 24;
            }

            return color;
        }

        private void HandleKeyboardInputs()
        {
            ImGuiIOPtr io = ImGuiNET.ImGui.GetIO();

            bool shift = io.KeyShift;
            bool ctrl = io.ConfigMacOSXBehaviors ? io.KeySuper : io.KeyCtrl;
            bool alt = io.ConfigMacOSXBehaviors ? io.KeyCtrl : io.KeyAlt;

            if (ImGuiNET.ImGui.IsWindowFocused())
            {
                if (ImGuiNET.ImGui.IsWindowHovered())
                    ImGuiNET.ImGui.SetMouseCursor(ImGuiMouseCursor.TextInput);

                io.WantCaptureKeyboard = true;
                io.WantTextInput = true;

                if (!IsReadOnly && ctrl && !shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Z))
                    Undo();
                else if (!IsReadOnly && !ctrl && !shift && alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Backspace))
                    Undo();
                else if (!IsReadOnly && ctrl && !shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Y))
                    Redo();
                else if (!ctrl && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.UpArrow))
                    MoveUp(1, shift);
                else if (!ctrl && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                    MoveDown(1, shift);
                else if (!alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.LeftArrow))
                    MoveLeft(1, shift, ctrl);
                else if (!alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.RightArrow))
                    MoveRight(1, shift, ctrl);
                else if (!alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.PageUp))
                    MoveUp(GetPageSize() - 4, shift);
                else if (!alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.PageDown))
                    MoveDown(GetPageSize() - 4, shift);
                else if (!alt && ctrl && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Home))
                    MoveTop(shift);
                else if (ctrl && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.End))
                    MoveBottom(shift);
                else if (!ctrl && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Home))
                    MoveHome(shift);
                else if (!ctrl && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.End))
                    MoveEnd(shift);
                else if (!IsReadOnly && !ctrl && !shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Delete))
                    Delete();
                else if (!IsReadOnly && !ctrl && !shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Backspace))
                    Backspace();
                else if (!ctrl && !shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Insert))
                    IsOverwrite ^= true;
                else if (ctrl && !shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Insert))
                    Copy();
                else if (ctrl && !shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.C))
                    Copy();
                else if (!IsReadOnly && !ctrl && shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Insert))
                    Paste();
                else if (!IsReadOnly && ctrl && !shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.V))
                    Paste();
                else if (ctrl && !shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.X))
                    Cut();
                else if (!ctrl && shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Delete))
                    Cut();
                else if (ctrl && !shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.A))
                    SelectAll();
                else if (!IsReadOnly && !ctrl && !shift && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Enter))
                    EnterCharacter('\n', false);
                else if (!IsReadOnly && !ctrl && !alt && ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Tab))
                    EnterCharacter('\t', shift);

                if (!IsReadOnly && io.InputQueueCharacters.Size > 0)
                {
                    for (var i = 0; i < io.InputQueueCharacters.Size; i++)
                    {
                        ushort c = io.InputQueueCharacters[i];
                        if (c != 0 && (c == '\n' || c >= 32))
                            EnterCharacter((char)c, shift);
                    }
                }
            }
        }

        private void HandleMouseInputs()
        {
            ImGuiIOPtr io = ImGuiNET.ImGui.GetIO();
            bool shift = io.KeyShift;
            bool ctrl = io.ConfigMacOSXBehaviors ? io.KeySuper : io.KeyCtrl;
            bool alt = io.ConfigMacOSXBehaviors ? io.KeyCtrl : io.KeyAlt;

            if (ImGuiNET.ImGui.IsWindowHovered())
            {
                if (!shift && !alt)
                {
                    double t = ImGuiNET.ImGui.GetTime();

                    bool click = ImGuiNET.ImGui.IsMouseClicked(0);
                    bool doubleClick = ImGuiNET.ImGui.IsMouseDoubleClicked(0);
                    bool tripleClick = click && !doubleClick && _lastClick != -1.0f && t - _lastClick < io.MouseDoubleClickTime;

                    /*
                    Left mouse button triple click
                    */

                    if (tripleClick)
                    {
                        if (!ctrl)
                        {
                            mState.CursorPosition = _interactiveStart =
                                _interactiveEnd = ScreenPosToCoordinates(ImGuiNET.ImGui.GetMousePos());
                            _selectionMode = SelectionMode.Line;
                            SetSelection(_interactiveStart, _interactiveEnd, _selectionMode);
                        }

                        _lastClick = -1.0f;
                    }

                    /*
                    Left mouse button double click
                    */

                    else if (doubleClick)
                    {
                        if (!ctrl)
                        {
                            mState.CursorPosition = _interactiveStart =
                                _interactiveEnd = ScreenPosToCoordinates(ImGuiNET.ImGui.GetMousePos());
                            if (_selectionMode == SelectionMode.Line)
                                _selectionMode = SelectionMode.Normal;
                            else
                                _selectionMode = SelectionMode.Word;
                            SetSelection(_interactiveStart, _interactiveEnd, _selectionMode);
                        }

                        _lastClick = (float)ImGuiNET.ImGui.GetTime();
                    }

                    /*
                    Left mouse button click
                    */

                    else if (click)
                    {
                        mState.CursorPosition =
                            _interactiveStart = _interactiveEnd = ScreenPosToCoordinates(ImGuiNET.ImGui.GetMousePos());
                        if (ctrl)
                            _selectionMode = SelectionMode.Word;
                        else
                            _selectionMode = SelectionMode.Normal;
                        SetSelection(_interactiveStart, _interactiveEnd, _selectionMode);

                        _lastClick = (float)ImGuiNET.ImGui.GetTime();
                    }
                    // Mouse left button dragging (=> update selection)
                    else if (ImGuiNET.ImGui.IsMouseDragging(0) && ImGuiNET.ImGui.IsMouseDown(0))
                    {
                        io.WantCaptureMouse = true;
                        mState.CursorPosition = _interactiveEnd = ScreenPosToCoordinates(ImGuiNET.ImGui.GetMousePos());
                        SetSelection(_interactiveStart, _interactiveEnd, _selectionMode);
                    }
                }
            }
        }

        private void Render()
        {
            /* Compute CharacterAdvance regarding to scaled font size (Ctrl + mouse wheel) */
            float fontSize = ImGuiNET.ImGui.GetFont().CalcTextSizeA(ImGuiNET.ImGui.GetFontSize(), float.MaxValue, -1.0f, "#").X;
            _characterAdvance = new Vector2(fontSize, ImGuiNET.ImGui.GetTextLineHeightWithSpacing() * LineSpacing);

            /* Update palette with the current alpha from style */
            for (var i = 0; i < (int)PaletteIndex.Max; ++i)
            {
                Vector4 color = ImGuiNET.ImGui.ColorConvertU32ToFloat4(_setPalette[i]);
                color.W *= ImGuiNET.ImGui.GetStyle().Alpha;
                _currentPalette[i] = ImGuiNET.ImGui.ColorConvertFloat4ToU32(color);
            }

            if (_lineBuffer != string.Empty)
                throw new InvalidOperationException("Data in line buffer.");

            Vector2 contentSize = ImGuiNET.ImGui.GetContentRegionAvail();
            ImDrawListPtr drawList = ImGuiNET.ImGui.GetWindowDrawList();
            float longest = _textStart;

            if (_scrollToTop)
            {
                _scrollToTop = false;
                ImGuiNET.ImGui.SetScrollY(0f);
            }

            Vector2 cursorScreenPos = ImGuiNET.ImGui.GetCursorScreenPos();
            float scrollX = ImGuiNET.ImGui.GetScrollX();
            float scrollY = ImGuiNET.ImGui.GetScrollY();

            var lineNo = (int)Math.Floor(scrollY / _characterAdvance.Y);
            int globalLineMax = _lines.Count;
            int lineMax = Math.Max(0,
                Math.Min(_lines.Count - 1, lineNo + (int)Math.Floor((scrollY + contentSize.Y) / _characterAdvance.Y)));

            // Deduce mTextStart by evaluating Lines size (global lineMax) plus two spaces as text width
            if (IsShowingLineNumbers)
                _textStart = ImGuiNET.ImGui.GetFont().CalcTextSizeA(ImGuiNET.ImGui.GetFontSize(), float.MaxValue, -1.0f, $" {globalLineMax} ").X +
                             LeftMargin;
            else
                _textStart = LeftMargin;

            if (_lines.Count > 0)
            {
                float spaceSize = ImGuiNET.ImGui.GetFont().CalcTextSizeA(ImGuiNET.ImGui.GetFontSize(), float.MaxValue, -1.0f, " ").X;

                while (lineNo <= lineMax)
                {
                    var lineStartScreenPos = new Vector2(cursorScreenPos.X, cursorScreenPos.Y + lineNo * _characterAdvance.Y);
                    var textScreenPos = new Vector2(lineStartScreenPos.X + _textStart, lineStartScreenPos.Y);

                    GlyphLine line = _lines[lineNo];
                    longest = Math.Max(_textStart + TextDistanceToLineStart(new Coordinate(lineNo, GetLineMaxColumn(lineNo))),
                        longest);
                    var columnNo = 0;
                    Coordinate lineStartCoord = new(lineNo, 0);
                    Coordinate lineEndCoord = new(lineNo, GetLineMaxColumn(lineNo));

                    // Draw selection for the current line
                    float sstart = -1.0f;
                    float ssend = -1.0f;

                    if (mState.SelectionStart > mState.SelectionEnd)
                        throw new InvalidOperationException("Invalid selection.");
                    if (mState.SelectionStart <= lineEndCoord)
                        sstart = mState.SelectionStart > lineStartCoord
                            ? TextDistanceToLineStart(mState.SelectionStart)
                            : 0.0f;
                    if (mState.SelectionEnd > lineStartCoord)
                        ssend = TextDistanceToLineStart(mState.SelectionEnd < lineEndCoord
                            ? mState.SelectionEnd
                            : lineEndCoord);

                    if (mState.SelectionEnd.Line > lineNo)
                        ssend += _characterAdvance.X;

                    if (sstart != -1 && ssend != -1 && sstart < ssend)
                    {
                        Vector2 vstart = new(lineStartScreenPos.X + _textStart + sstart, lineStartScreenPos.Y);
                        Vector2 vend = new(lineStartScreenPos.X + _textStart + ssend, lineStartScreenPos.Y + _characterAdvance.Y);
                        drawList.AddRectFilled(vstart, vend, _currentPalette[(int)PaletteIndex.Selection]);
                    }

                    // Draw breakpoints
                    var start = new Vector2(lineStartScreenPos.X + scrollX, lineStartScreenPos.Y);

                    if (_breakpoints.Contains(lineNo + 1))
                    {
                        var end = new Vector2(lineStartScreenPos.X + contentSize.X + 2.0f * scrollX,
                            lineStartScreenPos.Y + _characterAdvance.Y);
                        drawList.AddRectFilled(start, end, _currentPalette[(int)PaletteIndex.Breakpoint]);
                    }

                    // Draw error markers
                    if (_errorMarkers.TryGetValue(lineNo + 1, out string errorIt))
                    {
                        var end = new Vector2(lineStartScreenPos.X + contentSize.X + 2.0f * scrollX,
                            lineStartScreenPos.Y + _characterAdvance.Y);
                        drawList.AddRectFilled(start, end, _currentPalette[(int)PaletteIndex.ErrorMarker]);

                        if (ImGuiNET.ImGui.IsMouseHoveringRect(lineStartScreenPos, end))
                        {
                            ImGuiNET.ImGui.BeginTooltip();
                            ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.2f, 0.2f, 1.0f));
                            ImGuiNET.ImGui.Text($"Error at line {lineNo + 1}:");
                            ImGuiNET.ImGui.PopStyleColor();
                            ImGuiNET.ImGui.Separator();
                            ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.2f, 1.0f));
                            ImGuiNET.ImGui.Text(errorIt);
                            ImGuiNET.ImGui.PopStyleColor();
                            ImGuiNET.ImGui.EndTooltip();
                        }
                    }

                    // Draw line number (right aligned)
                    if (IsShowingLineNumbers)
                    {
                        var lineText = $"{lineNo + 1}  ";

                        float lineNoWidth =
                            ImGuiNET.ImGui.GetFont().CalcTextSizeA(ImGuiNET.ImGui.GetFontSize(), float.MaxValue, -1.0f, lineText).X;
                        drawList.AddText(new Vector2(lineStartScreenPos.X + _textStart - lineNoWidth, lineStartScreenPos.Y),
                            _currentPalette[(int)PaletteIndex.LineNumber], lineText);
                    }

                    if (mState.CursorPosition.Line == lineNo)
                    {
                        bool focused = ImGuiNET.ImGui.IsWindowFocused();

                        // Highlight the current line (where the cursor is)
                        if (!HasSelection())
                        {
                            var end = new Vector2(start.X + contentSize.X + scrollX, start.Y + _characterAdvance.Y);
                            drawList.AddRectFilled(start, end,
                                _currentPalette[
                                    (int)(focused
                                        ? PaletteIndex.CurrentLineFill
                                        : PaletteIndex.CurrentLineFillInactive)]);
                            drawList.AddRect(start, end, _currentPalette[(int)PaletteIndex.CurrentLineEdge], 1.0f);
                        }

                        // Render the cursor
                        if (focused)
                        {
                            double timeEnd = (DateTime.Now - DateTime.UnixEpoch).TotalMilliseconds;
                            double elapsed = timeEnd - _startTime;
                            if (elapsed > 400)
                            {
                                var width = 1.0f;
                                int cindex = GetCharacterIndex(mState.CursorPosition);
                                float cx = TextDistanceToLineStart(mState.CursorPosition);

                                if (IsOverwrite && cindex < line.Length)
                                {
                                    char c = line[cindex].Character;
                                    if (c == '\t')
                                    {
                                        float x = (1.0f + (float)Math.Floor((1.0f + cx) / (TabSize * spaceSize))) *
                                                  (TabSize * spaceSize);
                                        width = x - cx;
                                    }
                                    else
                                    {
                                        width = ImGuiNET.ImGui.GetFont().CalcTextSizeA(ImGuiNET.ImGui.GetFontSize(), float.MaxValue, -1.0f, $"{line[cindex].Character}").X;
                                    }
                                }

                                Vector2 cstart = new(textScreenPos.X + cx, lineStartScreenPos.Y);
                                Vector2 cend = new(textScreenPos.X + cx + width, lineStartScreenPos.Y + _characterAdvance.Y);
                                drawList.AddRectFilled(cstart, cend, _currentPalette[(int)PaletteIndex.Cursor]);
                                if (elapsed > 800)
                                    _startTime = timeEnd;
                            }
                        }
                    }

                    // Render colorized text
                    uint prevColor = line.Length <= 0 ? _currentPalette[(int)PaletteIndex.Default] : GetGlyphColor(line[0]);
                    Vector2 bufferOffset = Vector2.Zero;

                    for (var i = 0; i < line.Length;)
                    {
                        Glyph glyph = line[i];
                        uint color = GetGlyphColor(glyph);

                        if ((color != prevColor || glyph.Character == '\t' || glyph.Character == ' ') && _lineBuffer.Length > 0)
                        {
                            Vector2 newOffset = new(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y);
                            drawList.AddText(newOffset, prevColor, _lineBuffer);
                            Vector2 textSize = ImGuiNET.ImGui.GetFont().CalcTextSizeA(ImGuiNET.ImGui.GetFontSize(), float.MaxValue, -1.0f,
                                _lineBuffer);
                            bufferOffset.X += textSize.X;
                            _lineBuffer = string.Empty;
                        }

                        prevColor = color;

                        switch (glyph.Character)
                        {
                            case '\t':
                                {
                                    float oldX = bufferOffset.X;
                                    bufferOffset.X = (float)(1.0f + Math.Floor((1.0f + bufferOffset.X) / (TabSize * spaceSize))) * (TabSize * spaceSize);
                                    ++i;

                                    if (IsShowingWhitespaces)
                                    {
                                        float s = ImGuiNET.ImGui.GetFontSize();
                                        float x1 = textScreenPos.X + oldX + 1.0f;
                                        float x2 = textScreenPos.X + bufferOffset.X - 1.0f;
                                        float y = textScreenPos.Y + bufferOffset.Y + s * 0.5f;
                                        Vector2 p1 = new(x1, y);
                                        Vector2 p2 = new(x2, y);
                                        Vector2 p3 = new(x2 - s * 0.2f, y - s * 0.2f);
                                        Vector2 p4 = new(x2 - s * 0.2f, y + s * 0.2f);
                                        drawList.AddLine(p1, p2, 0x90909090);
                                        drawList.AddLine(p2, p3, 0x90909090);
                                        drawList.AddLine(p2, p4, 0x90909090);
                                    }

                                    break;
                                }
                            case ' ':
                                {
                                    if (IsShowingWhitespaces)
                                    {
                                        float s = ImGuiNET.ImGui.GetFontSize();
                                        float x = textScreenPos.X + bufferOffset.X + spaceSize * 0.5f;
                                        float y = textScreenPos.Y + bufferOffset.Y + s * 0.5f;
                                        drawList.AddCircleFilled(new Vector2(x, y), 1.5f, 0x80808080, 4);
                                    }

                                    bufferOffset.X += spaceSize;
                                    i++;
                                    break;
                                }
                            default:
                                _lineBuffer += line[i++].Character;
                                break;
                        }

                        ++columnNo;
                    }

                    if (_lineBuffer.Length > 0)
                    {
                        Vector2 newOffset = new(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y);
                        drawList.AddText(newOffset, prevColor, _lineBuffer);
                        _lineBuffer = string.Empty;
                    }

                    ++lineNo;
                }

                // Draw a tooltip on known identifiers/preprocessor symbols
                if (ImGuiNET.ImGui.IsMousePosValid())
                {
                    string id = GetWordAt(ScreenPosToCoordinates(ImGuiNET.ImGui.GetMousePos()));
                    if (id.Length > 0)
                    {
                        if (_languageDefinition.Identifiers.TryGetValue(id, out Identifier it))
                        {
                            ImGuiNET.ImGui.BeginTooltip();
                            ImGuiNET.ImGui.TextUnformatted(it.Value);
                            ImGuiNET.ImGui.EndTooltip();
                        }
                        else
                        {
                            if (_languageDefinition.Preprocessors.TryGetValue(id, out Identifier pi))
                            {
                                ImGuiNET.ImGui.BeginTooltip();
                                ImGuiNET.ImGui.TextUnformatted(pi.Value);
                                ImGuiNET.ImGui.EndTooltip();
                            }
                        }
                    }
                }
            }

            ImGuiNET.ImGui.Dummy(new Vector2(longest + 2, _lines.Count * _characterAdvance.Y));

            if (_scrollToSelection)
            {
                EnsureSelectionVisible();
                _scrollToSelection = false;
            }
            else if (_scrollToCursor)
            {
                EnsureCursorVisible();
                ImGuiNET.ImGui.SetWindowFocus();
                _scrollToCursor = false;
            }
        }

        private void Render(string aTitle, Vector2 aSize, bool aBorder)
        {
            _withinRender = true;
            _isTextChanged = false;
            _isCursorPositionChanged = false;

            ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ChildBg,
                ImGuiNET.ImGui.ColorConvertU32ToFloat4(_currentPalette[(int)PaletteIndex.Background]));
            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

            ImGuiNET.ImGui.BeginChild(aTitle, aSize, aBorder ? ImGuiChildFlags.Border : ImGuiChildFlags.None,
                ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoMove);

            if (IsHandleKeyboardInputsEnabled)
                HandleKeyboardInputs();

            if (IsHandleMouseInputsEnabled)
                HandleMouseInputs();

            ColorizeInternal();
            Render();

            ImGuiNET.ImGui.EndChild();

            ImGuiNET.ImGui.PopStyleVar();
            ImGuiNET.ImGui.PopStyleColor();

            _withinRender = false;

            if (_isTextChanged)
                TextChanged?.Invoke(this, GetText());

            if (_isCursorPositionChanged)
                CursorPositionChanged?.Invoke(this, mState.CursorPosition);
        }

        public override Size GetSize() => Size.Parent;

        protected override void UpdateInternal(Rectangle contentRect)
        {
            lock (_lock)
                Render(string.Empty, contentRect.Size, false);
        }

        public void SetText(string aText)
        {
            lock (_lock)
            {
                _lines.Clear();
                _lines.Add(new GlyphLine());

                foreach (char chr in aText)
                {
                    switch (chr)
                    {
                        case '\r':
                            // ignore the carriage return character
                            _lines[^1].HasCarriageReturn = true;
                            break;

                        case '\n':
                            _lines.Add(new GlyphLine());
                            break;

                        default:
                            _lines[^1].Glyphs.Add(new Glyph(chr));
                            break;
                    }
                }

                _isTextChanged = true;
                _scrollToTop = true;

                _undoBuffer.Clear();
                _undoIndex = 0;

                Colorize();
            }
        }

        public void SetTextLines(List<string> aLines)
        {
            lock (_lock)
            {
                _lines.Clear();

                if (aLines.Count <= 0)
                {
                    _lines.Add(new GlyphLine());
                }
                else
                {
                    _lines = new List<GlyphLine>();

                    for (var i = 0; i < aLines.Count; ++i)
                    {
                        string aLine = aLines[i];

                        _lines.Add(new GlyphLine());
                        foreach (char lineChar in aLine)
                            _lines[i].Glyphs.Add(new Glyph(lineChar));
                    }
                }

                _isTextChanged = true;
                _scrollToTop = true;

                _undoBuffer.Clear();
                _undoIndex = 0;

                Colorize();
            }
        }

        private void EnterCharacter(char aChar, bool aShift)
        {
            if (IsReadOnly)
                return;

            UndoRecord u = new();

            u.mBefore = mState;

            if (HasSelection())
            {
                if (aChar == '\t' && mState.SelectionStart.Line != mState.SelectionEnd.Line)
                {
                    Coordinate start = mState.SelectionStart;
                    Coordinate end = mState.SelectionEnd;
                    Coordinate originalEnd = end;

                    if (start > end)
                        (start, end) = (end, start);

                    start.Column = 0;
                    //			end.Column = end.Line < Lines.Count ? Lines[end.Line].Count : 0;
                    if (end.Column == 0 && end.Line > 0)
                        --end.Line;
                    if (end.Line >= _lines.Count)
                        end.Line = _lines.Count <= 0 ? 0 : _lines.Count - 1;
                    end.Column = GetLineMaxColumn(end.Line);

                    //if (end.Column >= GetLineMaxColumn(end.Line))
                    //	end.Column = GetLineMaxColumn(end.Line) - 1;

                    u.mRemovedStart = start;
                    u.mRemovedEnd = end;
                    u.mRemoved = GetText(start, end);

                    var modified = false;

                    for (int i = start.Line; i <= end.Line; i++)
                    {
                        GlyphLine line = _lines[i];
                        if (aShift)
                        {
                            if (line.Length > 0)
                            {
                                if (line[0].Character == '\t')
                                {
                                    line.Glyphs.RemoveAt(0);
                                    modified = true;
                                }
                                else
                                {
                                    for (var j = 0; j < TabSize && line.Length > 0 && line[0].Character == ' '; j++)
                                    {
                                        line.Glyphs.RemoveAt(0);
                                        modified = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            line.Glyphs.Insert(0, new Glyph('\t', PaletteIndex.Background));
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        start = new Coordinate(start.Line, GetCharacterColumn(start.Line, 0));
                        Coordinate rangeEnd;
                        if (originalEnd.Column != 0)
                        {
                            end = new Coordinate(end.Line, GetLineMaxColumn(end.Line));
                            rangeEnd = end;
                            u.mAdded = GetText(start, end);
                        }
                        else
                        {
                            end = new Coordinate(originalEnd.Line, 0);
                            rangeEnd = new Coordinate(end.Line - 1, GetLineMaxColumn(end.Line - 1));
                            u.mAdded = GetText(start, rangeEnd);
                        }

                        u.mAddedStart = start;
                        u.mAddedEnd = rangeEnd;
                        u.mAfter = mState;

                        mState.SelectionStart = start;
                        mState.SelectionEnd = end;
                        AddUndo(u);

                        _isTextChanged = true;

                        EnsureCursorVisible();
                    }

                    return;
                } // c == '\t'
                else
                {
                    u.mRemoved = GetSelectedText();
                    u.mRemovedStart = mState.SelectionStart;
                    u.mRemovedEnd = mState.SelectionEnd;
                    DeleteSelection();
                }
            } // HasSelection

            Coordinate coord = GetCursorPosition();
            u.mAddedStart = coord;

            if (_lines.Count <= 0)
                throw new InvalidOperationException("No more lines left.");

            if (aChar == '\n')
            {
                InsertLine(coord.Line + 1);
                GlyphLine line = _lines[coord.Line];
                GlyphLine newLine = _lines[coord.Line + 1];

                if (_languageDefinition.IsAutoIndentation)
                    for (var it = 0; it < line.Length && char.IsAscii(line[it].Character) && char.IsWhiteSpace(line[it].Character); ++it)
                        newLine.Glyphs.Add(line[it]);

                int whitespaceSize = newLine.Length;
                int cindex = GetCharacterIndex(coord);
                newLine.Glyphs.AddRange(line.Glyphs[cindex..]);
                line.Glyphs.RemoveRange(cindex, line.Length - cindex);
                SetCursorPosition(new Coordinate(coord.Line + 1, GetCharacterColumn(coord.Line + 1, whitespaceSize)));
                u.mAdded = $"{aChar}";
            }
            else
            {
                GlyphLine line = _lines[coord.Line];
                int cindex = GetCharacterIndex(coord);

                if (IsOverwrite && cindex < line.Length)
                {
                    u.mRemovedStart = mState.CursorPosition;
                    u.mRemovedEnd = new Coordinate(coord.Line, GetCharacterColumn(coord.Line, cindex + 1));

                    u.mRemoved += line[cindex].Character;
                    line.Glyphs.RemoveAt(cindex);
                }

                line.Glyphs.Insert(cindex, new Glyph(aChar));

                cindex++;
                u.mAdded = $"{aChar}";

                SetCursorPosition(new Coordinate(coord.Line, GetCharacterColumn(coord.Line, cindex)));
            }

            _isTextChanged = true;

            u.mAddedEnd = GetCursorPosition();
            u.mAfter = mState;

            AddUndo(u);

            Colorize(coord.Line - 1, 3);
            EnsureCursorVisible();
        }

        public void SetCursorPosition(Coordinate aPosition)
        {
            if (mState.CursorPosition != aPosition)
            {
                mState.CursorPosition = aPosition;
                _isCursorPositionChanged = true;
                EnsureCursorVisible();
            }
        }

        public void SetSelectionStart(Coordinate aPosition)
        {
            mState.SelectionStart = SanitizeCoordinates(aPosition);
            if (mState.SelectionStart > mState.SelectionEnd)
                (mState.SelectionStart, mState.SelectionEnd) = (mState.SelectionEnd, mState.SelectionStart);
        }

        public void SetSelectionEnd(Coordinate aPosition)
        {
            mState.SelectionEnd = SanitizeCoordinates(aPosition);
            if (mState.SelectionStart > mState.SelectionEnd)
                (mState.SelectionStart, mState.SelectionEnd) = (mState.SelectionEnd, mState.SelectionStart);
        }

        public void SetSelection(Coordinate aStart, Coordinate aEnd, SelectionMode aMode = SelectionMode.Normal)
        {
            Coordinate oldSelStart = mState.SelectionStart;
            Coordinate oldSelEnd = mState.SelectionEnd;

            mState.SelectionStart = SanitizeCoordinates(aStart);
            mState.SelectionEnd = SanitizeCoordinates(aEnd);
            if (mState.SelectionStart > mState.SelectionEnd)
                (mState.SelectionStart, mState.SelectionEnd) = (mState.SelectionEnd, mState.SelectionStart);

            switch (aMode)
            {
                case SelectionMode.Normal:
                    break;
                case SelectionMode.Word:
                    {
                        mState.SelectionStart = FindWordStart(mState.SelectionStart);
                        if (!IsOnWordBoundary(mState.SelectionEnd))
                            mState.SelectionEnd = FindWordEnd(FindWordStart(mState.SelectionEnd));
                        break;
                    }
                case SelectionMode.Line:
                    {
                        int lineNo = mState.SelectionEnd.Line;
                        int lineSize = lineNo < _lines.Count ? _lines[lineNo].Length : 0;
                        mState.SelectionStart = new Coordinate(mState.SelectionStart.Line, 0);
                        mState.SelectionEnd = new Coordinate(lineNo, GetLineMaxColumn(lineNo));
                        break;
                    }
                default:
                    break;
            }

            if (mState.SelectionStart != oldSelStart ||
                mState.SelectionEnd != oldSelEnd)
                _isCursorPositionChanged = true;
        }

        private void InsertText(string aValue)
        {
            if (aValue == null)
                return;

            Coordinate pos = GetCursorPosition();
            Coordinate start = pos < mState.SelectionStart ? pos : mState.SelectionStart;
            int totalLines = pos.Line - start.Line;

            totalLines += InsertTextAt(ref pos, aValue);

            SetSelection(pos, pos);
            SetCursorPosition(pos);
            Colorize(start.Line - 1, totalLines + 2);
        }

        private void DeleteSelection()
        {
            if (mState.SelectionEnd < mState.SelectionStart)
                throw new InvalidOperationException("Invalid selection range.");

            if (mState.SelectionEnd == mState.SelectionStart)
                return;

            DeleteRange(mState.SelectionStart, mState.SelectionEnd);

            SetSelection(mState.SelectionStart, mState.SelectionStart);
            SetCursorPosition(mState.SelectionStart);
            Colorize(mState.SelectionStart.Line, 1);
        }

        public void MoveUp(int aAmount = 1, bool aSelect = false)
        {
            Coordinate oldPos = mState.CursorPosition;
            oldPos.Line = Math.Max(0, mState.CursorPosition.Line - aAmount);

            if (oldPos != mState.CursorPosition)
            {
                if (aSelect)
                {
                    if (oldPos == _interactiveStart)
                        _interactiveStart = mState.CursorPosition;
                    else if (oldPos == _interactiveEnd)
                        _interactiveEnd = mState.CursorPosition;
                    else
                    {
                        _interactiveStart = mState.CursorPosition;
                        _interactiveEnd = oldPos;
                    }
                }
                else
                    _interactiveStart = _interactiveEnd = mState.CursorPosition;

                SetSelection(_interactiveStart, _interactiveEnd);

                EnsureCursorVisible();
            }

            mState.CursorPosition = oldPos;
        }

        public void MoveDown(int aAmount = 1, bool aSelect = false)
        {
            if (mState.CursorPosition.Column < 0)
                throw new InvalidOperationException("Invalid cursor position.");

            Coordinate oldPos = mState.CursorPosition;
            oldPos.Line = Math.Max(0, Math.Min(_lines.Count - 1, oldPos.Line + aAmount));

            if (mState.CursorPosition != oldPos)
            {
                if (aSelect)
                {
                    if (oldPos == _interactiveEnd)
                        _interactiveEnd = mState.CursorPosition;
                    else if (oldPos == _interactiveStart)
                        _interactiveStart = mState.CursorPosition;
                    else
                    {
                        _interactiveStart = oldPos;
                        _interactiveEnd = mState.CursorPosition;
                    }
                }
                else
                    _interactiveStart = _interactiveEnd = mState.CursorPosition;

                SetSelection(_interactiveStart, _interactiveEnd);

                EnsureCursorVisible();
            }

            mState.CursorPosition = oldPos;
        }

        public void MoveLeft(int aAmount = 1, bool aSelect = false, bool aWordMode = false)
        {
            if (_lines.Count <= 0)
                return;

            Coordinate oldPos = mState.CursorPosition;
            mState.CursorPosition = GetCursorPosition();
            int line = mState.CursorPosition.Line;
            int cindex = GetCharacterIndex(mState.CursorPosition);

            while (aAmount-- > 0)
            {
                if (cindex == 0)
                {
                    if (line > 0)
                    {
                        --line;
                        if (_lines.Count > line)
                            cindex = _lines[line].Length;
                        else
                            cindex = 0;
                    }
                }
                else
                {
                    --cindex;
                }

                mState.CursorPosition = new Coordinate(line, GetCharacterColumn(line, cindex));
                if (aWordMode)
                {
                    mState.CursorPosition = FindWordStart(mState.CursorPosition);
                    cindex = GetCharacterIndex(mState.CursorPosition);
                }
            }

            mState.CursorPosition = new Coordinate(line, GetCharacterColumn(line, cindex));

            if (mState.CursorPosition.Column < 0)
                throw new InvalidOperationException("Invalid cursor position.");

            if (aSelect)
            {
                if (oldPos == _interactiveStart)
                    _interactiveStart = mState.CursorPosition;
                else if (oldPos == _interactiveEnd)
                    _interactiveEnd = mState.CursorPosition;
                else
                {
                    _interactiveStart = mState.CursorPosition;
                    _interactiveEnd = oldPos;
                }
            }
            else
                _interactiveStart = _interactiveEnd = mState.CursorPosition;

            SetSelection(_interactiveStart, _interactiveEnd,
                aSelect && aWordMode ? SelectionMode.Word : SelectionMode.Normal);

            EnsureCursorVisible();
        }

        public void MoveRight(int aAmount = 1, bool aSelect = false, bool aWordMode = false)
        {
            Coordinate cursorPos = mState.CursorPosition;

            if (_lines.Count <= 0 || cursorPos.Line >= _lines.Count)
                return;

            int cindex = GetCharacterIndex(cursorPos);
            while (aAmount-- > 0)
            {
                int lindex = cursorPos.Line;
                GlyphLine line = _lines[lindex];

                if (cindex >= line.Length)
                {
                    if (cursorPos.Line < _lines.Count - 1)
                    {
                        cursorPos.Line = Math.Max(0, Math.Min(_lines.Count - 1, cursorPos.Line + 1));
                        cursorPos.Column = 0;
                    }
                    else
                    {
                        mState.CursorPosition = cursorPos;

                        return;
                    }
                }
                else
                {
                    if (!aWordMode)
                        cindex++;

                    cursorPos = new Coordinate(lindex, GetCharacterColumn(lindex, cindex));

                    if (aWordMode)
                        cursorPos = FindWordEnd(cursorPos);
                }
            }

            if (aSelect)
            {
                if (cursorPos == _interactiveEnd)
                    _interactiveEnd = SanitizeCoordinates(cursorPos);
                else if (cursorPos == _interactiveStart)
                    _interactiveStart = cursorPos;
                else
                {
                    _interactiveStart = cursorPos;
                    _interactiveEnd = cursorPos;
                }
            }
            else
                _interactiveStart = _interactiveEnd = cursorPos;

            SetSelection(_interactiveStart, _interactiveEnd,
                aSelect && aWordMode ? SelectionMode.Word : SelectionMode.Normal);

            EnsureCursorVisible();

            mState.CursorPosition = cursorPos;
        }

        public void MoveTop(bool aSelect = false)
        {
            Coordinate oldPos = mState.CursorPosition;
            SetCursorPosition(new Coordinate(0, 0));

            if (mState.CursorPosition != oldPos)
            {
                if (aSelect)
                {
                    _interactiveEnd = oldPos;
                    _interactiveStart = mState.CursorPosition;
                }
                else
                    _interactiveStart = _interactiveEnd = mState.CursorPosition;

                SetSelection(_interactiveStart, _interactiveEnd);
            }
        }

        public void MoveBottom(bool aSelect = false)
        {
            Coordinate oldPos = GetCursorPosition();
            var newPos = new Coordinate(_lines.Count - 1, 0);
            SetCursorPosition(newPos);
            if (aSelect)
            {
                _interactiveStart = oldPos;
                _interactiveEnd = newPos;
            }
            else
                _interactiveStart = _interactiveEnd = newPos;

            SetSelection(_interactiveStart, _interactiveEnd);
        }

        public void MoveHome(bool aSelect = false)
        {
            Coordinate oldPos = mState.CursorPosition;
            SetCursorPosition(new Coordinate(mState.CursorPosition.Line, 0));

            if (mState.CursorPosition != oldPos)
            {
                if (aSelect)
                {
                    if (oldPos == _interactiveStart)
                        _interactiveStart = mState.CursorPosition;
                    else if (oldPos == _interactiveEnd)
                        _interactiveEnd = mState.CursorPosition;
                    else
                    {
                        _interactiveStart = mState.CursorPosition;
                        _interactiveEnd = oldPos;
                    }
                }
                else
                    _interactiveStart = _interactiveEnd = mState.CursorPosition;

                SetSelection(_interactiveStart, _interactiveEnd);
            }
        }

        public void MoveEnd(bool aSelect = false)
        {
            Coordinate oldPos = mState.CursorPosition;
            SetCursorPosition(new Coordinate(mState.CursorPosition.Line, GetLineMaxColumn(oldPos.Line)));

            if (mState.CursorPosition != oldPos)
            {
                if (aSelect)
                {
                    if (oldPos == _interactiveEnd)
                        _interactiveEnd = mState.CursorPosition;
                    else if (oldPos == _interactiveStart)
                        _interactiveStart = mState.CursorPosition;
                    else
                    {
                        _interactiveStart = oldPos;
                        _interactiveEnd = mState.CursorPosition;
                    }
                }
                else
                    _interactiveStart = _interactiveEnd = mState.CursorPosition;

                SetSelection(_interactiveStart, _interactiveEnd);
            }
        }

        public void Delete()
        {
            if (IsReadOnly)
                return;

            if (_lines.Count <= 0)
                return;

            UndoRecord u = new();
            u.mBefore = mState;

            if (HasSelection())
            {
                u.mRemoved = GetSelectedText();
                u.mRemovedStart = mState.SelectionStart;
                u.mRemovedEnd = mState.SelectionEnd;

                DeleteSelection();
            }
            else
            {
                Coordinate pos = GetCursorPosition();
                SetCursorPosition(pos);
                GlyphLine line = _lines[pos.Line];

                if (pos.Column == GetLineMaxColumn(pos.Line))
                {
                    if (pos.Line == _lines.Count - 1)
                        return;

                    u.mRemoved = "\n";
                    u.mRemovedStart = u.mRemovedEnd = GetCursorPosition();
                    Advance(ref u.mRemovedEnd);

                    GlyphLine nextLine = _lines[pos.Line + 1];
                    line.Glyphs.AddRange(nextLine.Glyphs);
                    RemoveLine(pos.Line + 1);
                }
                else
                {
                    int cindex = GetCharacterIndex(pos);
                    u.mRemovedStart = u.mRemovedEnd = GetCursorPosition();
                    u.mRemovedEnd.Column++;
                    u.mRemoved = GetText(u.mRemovedStart, u.mRemovedEnd);

                    line.Glyphs.RemoveAt(cindex);
                }

                _isTextChanged = true;

                Colorize(pos.Line, 1);
            }

            u.mAfter = mState;
            AddUndo(u);
        }

        private void Backspace()
        {
            if (IsReadOnly)
                return;

            if (_lines.Count <= 0)
                return;

            UndoRecord u = new();
            u.mBefore = mState;

            if (HasSelection())
            {
                u.mRemoved = GetSelectedText();
                u.mRemovedStart = mState.SelectionStart;
                u.mRemovedEnd = mState.SelectionEnd;

                DeleteSelection();
            }
            else
            {
                Coordinate pos = GetActualCursorCoordinates();
                SetCursorPosition(pos);

                if (mState.CursorPosition.Column == 0)
                {
                    if (mState.CursorPosition.Line == 0)
                        return;

                    u.mRemoved = "\n";
                    u.mRemovedStart = u.mRemovedEnd = new Coordinate(pos.Line - 1, GetLineMaxColumn(pos.Line - 1));
                    Advance(ref u.mRemovedEnd);

                    GlyphLine line = _lines[mState.CursorPosition.Line];
                    GlyphLine prevLine = _lines[mState.CursorPosition.Line - 1];
                    int prevSize = GetLineMaxColumn(mState.CursorPosition.Line - 1);
                    prevLine.Glyphs.AddRange(line.Glyphs);

                    Dictionary<int, string> etmp = new();
                    foreach (KeyValuePair<int, string> i in _errorMarkers)
                        etmp[i.Key - 1 == mState.CursorPosition.Line ? i.Key - 1 : i.Key] = i.Value;
                    _errorMarkers = etmp;

                    RemoveLine(mState.CursorPosition.Line);

                    Coordinate newPos = mState.CursorPosition;
                    newPos.Line--;
                    newPos.Column = prevSize;
                    mState.CursorPosition = newPos;
                }
                else
                {
                    GlyphLine line = _lines[mState.CursorPosition.Line];
                    int cindex = GetCharacterIndex(pos) - 1;
                    int cend = cindex + 1;

                    u.mRemovedStart = u.mRemovedEnd = GetCursorPosition();
                    --u.mRemovedStart.Column;
                    SetCursorPosition(new Coordinate(mState.CursorPosition.Line, GetCharacterColumn(mState.CursorPosition.Line, cindex)));

                    while (cindex < line.Length && cend-- > cindex)
                    {
                        u.mRemoved += line[cindex].Character;
                        line.Glyphs.RemoveAt(cindex);
                    }
                }

                _isTextChanged = true;

                EnsureCursorVisible();
                Colorize(mState.CursorPosition.Line, 1);
            }

            u.mAfter = mState;
            AddUndo(u);
        }

        public void SelectWordUnderCursor()
        {
            Coordinate c = GetCursorPosition();
            SetSelection(FindWordStart(c), FindWordEnd(c));
        }

        public void SelectAll()
        {
            SetSelection(new Coordinate(0, 0), new Coordinate(_lines.Count, 0));
        }

        public bool HasSelection()
        {
            return mState.SelectionEnd > mState.SelectionStart;
        }

        public void Copy()
        {
            if (HasSelection())
            {
                ImGuiNET.ImGui.SetClipboardText(GetSelectedText());
            }
            else
            {
                if (_lines.Count > 0)
                {
                    var str = string.Empty;
                    GlyphLine line = _lines[GetCursorPosition().Line];
                    foreach (Glyph g in line.Glyphs)
                        str += g.Character;
                    ImGuiNET.ImGui.SetClipboardText(str);
                }
            }
        }

        public void Cut()
        {
            if (IsReadOnly)
            {
                Copy();
            }
            else
            {
                if (HasSelection())
                {
                    UndoRecord u = new();
                    u.mBefore = mState;
                    u.mRemoved = GetSelectedText();
                    u.mRemovedStart = mState.SelectionStart;
                    u.mRemovedEnd = mState.SelectionEnd;

                    Copy();
                    DeleteSelection();

                    u.mAfter = mState;
                    AddUndo(u);
                }
            }
        }

        public void Paste()
        {
            if (IsReadOnly)
                return;

            string clipText = ImGuiNET.ImGui.GetClipboardText();
            if (clipText != null && clipText.Length > 0)
            {
                UndoRecord u = new();
                u.mBefore = mState;

                if (HasSelection())
                {
                    u.mRemoved = GetSelectedText();
                    u.mRemovedStart = mState.SelectionStart;
                    u.mRemovedEnd = mState.SelectionEnd;
                    DeleteSelection();
                }

                u.mAdded = clipText;
                u.mAddedStart = GetCursorPosition();

                InsertText(clipText);

                u.mAddedEnd = GetCursorPosition();
                u.mAfter = mState;
                AddUndo(u);
            }
        }

        public bool CanUndo()
        {
            return !IsReadOnly && _undoIndex > 0;
        }

        public bool CanRedo()
        {
            return !IsReadOnly && _undoIndex < _undoBuffer.Count;
        }

        public void Undo(int aSteps = 1)
        {
            while (CanUndo() && aSteps-- > 0)
                _undoBuffer[--_undoIndex].Undo(this);
        }

        public void Redo(int aSteps = 1)
        {
            while (CanRedo() && aSteps-- > 0)
                _undoBuffer[_undoIndex++].Redo(this);
        }

        public static uint[] GetDarkPalette()
        {
            return new uint[]
            {
            0xff7f7f7f, // Default
            0xffd69c56, // Keyword	
            0xff00ff00, // Number
            0xff7070e0, // String
            0xff70a0e0, // Char literal
            0xffffffff, // Punctuation
            0xff408080, // Preprocessor
            0xffaaaaaa, // Identifier
            0xff9bc64d, // Known identifier
            0xffc040a0, // Preproc identifier
            0xff206020, // Comment (single line)
            0xff406020, // Comment (multi line)
            0xff101010, // Background
            0xffe0e0e0, // Cursor
            0x80a06020, // Selection
            0x800020ff, // ErrorMarker
            0x40f08000, // Breakpoint
            0xff707000, // Line number
            0x40000000, // Current line fill
            0x40808080, // Current line fill (inactive)
            0x40a0a0a0, // Current line edge
            };
        }

        public static uint[] GetLightPalette()
        {
            return new uint[]
            {
            0xff7f7f7f, // None
            0xffff0c06, // Keyword	
            0xff008000, // Number
            0xff2020a0, // String
            0xff304070, // Char literal
            0xff000000, // Punctuation
            0xff406060, // Preprocessor
            0xff404040, // Identifier
            0xff606010, // Known identifier
            0xffc040a0, // Preproc identifier
            0xff205020, // Comment (single line)
            0xff405020, // Comment (multi line)
            0xffffffff, // Background
            0xff000000, // Cursor
            0x80600000, // Selection
            0xa00010ff, // ErrorMarker
            0x80f08000, // Breakpoint
            0xff505000, // Line number
            0x40000000, // Current line fill
            0x40808080, // Current line fill (inactive)
            0x40000000, // Current line edge
            };
        }

        public static uint[] GetRetroBluePalette()
        {
            return new uint[]
            {
            0xff00ffff, // None
            0xffffff00, // Keyword	
            0xff00ff00, // Number
            0xff808000, // String
            0xff808000, // Char literal
            0xffffffff, // Punctuation
            0xff008000, // Preprocessor
            0xff00ffff, // Identifier
            0xffffffff, // Known identifier
            0xffff00ff, // Preproc identifier
            0xff808080, // Comment (single line)
            0xff404040, // Comment (multi line)
            0xff800000, // Background
            0xff0080ff, // Cursor
            0x80ffff00, // Selection
            0xa00000ff, // ErrorMarker
            0x80ff8000, // Breakpoint
            0xff808000, // Line number
            0x40000000, // Current line fill
            0x40808080, // Current line fill (inactive)
            0x40000000, // Current line edge
            };
        }

        public string GetText()
        {
            return GetText(new Coordinate(0, 0), new Coordinate(_lines.Count, 0));
        }

        public List<string> GetTextLines()
        {
            List<string> result = new(_lines.Count);

            foreach (GlyphLine line in _lines)
            {
                var text = string.Empty;

                for (var i = 0; i < line.Length; ++i)
                    text += line[i].Character;

                result.Add(text);
            }

            return result;
        }

        public int GetTotalLines() => _lines.Count;

        public string GetSelectedText()
        {
            return GetText(mState.SelectionStart, mState.SelectionEnd);
        }

        public string GetCurrentLineText()
        {
            int lineLength = GetLineMaxColumn(mState.CursorPosition.Line);
            return GetText(
                new Coordinate(mState.CursorPosition.Line, 0),
                new Coordinate(mState.CursorPosition.Line, lineLength));
        }

        internal void Colorize(int aFroLine = 0, int aLines = -1)
        {
            int toLine = aLines == -1 ? _lines.Count : Math.Min(_lines.Count, aFroLine + aLines);
            _colorRangeMin = Math.Min(_colorRangeMin, aFroLine);
            _colorRangeMax = Math.Max(_colorRangeMax, toLine);
            _colorRangeMin = Math.Max(0, _colorRangeMin);
            _colorRangeMax = Math.Max(_colorRangeMin, _colorRangeMax);
            _isCheckComments = true;
        }

        private void ColorizeRange(int aFroLine, int aToLine)
        {
            if (_lines.Count <= 0 || aFroLine >= aToLine)
                return;

            int endLine = Math.Max(0, Math.Min(_lines.Count, aToLine));
            for (int i = aFroLine; i < endLine; ++i)
            {
                GlyphLine line = _lines[i];

                if (line.Length <= 0)
                    continue;

                var buffer = string.Empty;
                for (var j = 0; j < line.Length; ++j)
                {
                    Glyph col = line[j];
                    buffer += col.Character;
                    col.ColorIndex = PaletteIndex.Default;
                    line[j] = col;
                }

                var bufferBegin = 0;
                int bufferEnd = buffer.Length;

                int last = bufferEnd;

                for (int first = bufferBegin; first != last;)
                {
                    bool hasTokenizeResult = _languageDefinition.Tokenize(buffer, first, last, out int token_begin, out int token_end, out PaletteIndex token_color);

                    if (!hasTokenizeResult)
                    {
                        foreach ((Regex, PaletteIndex) p in _languageRegexList)
                        {
                            Match results = p.Item1.Match(buffer, first, last - first);
                            if (results.Success)
                            {
                                hasTokenizeResult = true;

                                Match v = results;
                                token_begin = v.Index;
                                token_end = v.Length + v.Index;
                                token_color = p.Item2;
                                break;
                            }
                        }
                    }

                    if (!hasTokenizeResult)
                    {
                        first++;
                    }
                    else
                    {
                        int token_length = token_end - token_begin;

                        if (token_color == PaletteIndex.Identifier)
                        {
                            string id = buffer[token_begin..token_end];

                            // todo : allmost all language definitions use lower case to specify keywords, so shouldn't this use ::tolower ?
                            if (!_languageDefinition.IsCaseSensitive)
                                id = id.ToUpper();

                            if (!line[first - bufferBegin].IsPreprocessor)
                            {
                                if (_languageDefinition.Keywords.Contains(id))
                                    token_color = PaletteIndex.Keyword;
                                else if (_languageDefinition.Identifiers.ContainsKey(id))
                                    token_color = PaletteIndex.KnownIdentifier;
                                else if (_languageDefinition.Preprocessors.ContainsKey(id))
                                    token_color = PaletteIndex.PreprocessorIdentifier;
                            }
                            else
                            {
                                if (_languageDefinition.Preprocessors.ContainsKey(id))
                                    token_color = PaletteIndex.PreprocessorIdentifier;
                            }
                        }

                        for (var j = 0; j < token_length; ++j)
                        {
                            Glyph glyph = line[token_begin - bufferBegin + j];
                            glyph.ColorIndex = token_color;
                            line[token_begin - bufferBegin + j] = glyph;
                        }

                        first = token_end;
                    }
                }
            }
        }

        private void ColorizeInternal()
        {
            if (_lines.Count <= 0 || !IsColorizerEnabled)
                return;

            if (_isCheckComments)
            {
                int endLine = _lines.Count;
                var endIndex = 0;
                int commentStartLine = endLine;
                int commentStartIndex = endIndex;
                var withinString = false;
                var withinSingleLineComment = false;
                var withinPreproc = false;
                var firstChar = true; // there is no other non-whitespace characters in the line before
                var concatenate = false; // '\' on the very end of the line
                var currentLine = 0;
                var currentIndex = 0;
                while (currentLine < endLine || currentIndex < endIndex)
                {
                    GlyphLine line = _lines[currentLine];

                    if (currentIndex == 0 && !concatenate)
                    {
                        withinSingleLineComment = false;
                        withinPreproc = false;
                        firstChar = true;
                    }

                    concatenate = false;

                    if (line.Length > 0)
                    {
                        Glyph g = line[currentIndex];
                        char c = g.Character;

                        if (c != _languageDefinition.PreprocessorCharacter && !char.IsWhiteSpace(c))
                            firstChar = false;

                        if (currentIndex == line.Length - 1 && line[line.Length - 1].Character == '\\')
                            concatenate = true;

                        bool inComment = commentStartLine < currentLine ||
                                          commentStartLine == currentLine && commentStartIndex <= currentIndex;

                        Glyph currentGlyph = line[currentIndex];

                        if (withinString)
                        {
                            currentGlyph.IsMultiLineComment = inComment;

                            switch (c)
                            {
                                case '\"' when currentIndex + 1 < line.Length && line[currentIndex + 1].Character == '\"':
                                    {
                                        currentIndex += 1;
                                        if (currentIndex < line.Length)
                                            currentGlyph.IsMultiLineComment = inComment;
                                        break;
                                    }
                                case '\"':
                                    withinString = false;
                                    break;
                                case '\\':
                                    {
                                        currentIndex += 1;
                                        if (currentIndex < line.Length)
                                            currentGlyph.IsMultiLineComment = inComment;
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            if (firstChar && c == _languageDefinition.PreprocessorCharacter)
                                withinPreproc = true;

                            if (c == '\"')
                            {
                                withinString = true;
                                currentGlyph.IsMultiLineComment = inComment;
                            }
                            else
                            {
                                int from = currentIndex;
                                string startStr = _languageDefinition.CommentStartIdentifier;
                                string singleStartStr = _languageDefinition.SingleLineCommentIdentifier;

                                if (singleStartStr.Length > 0 &&
                                    currentIndex + singleStartStr.Length <= line.Length &&
                                    singleStartStr.Select((c, i) => line[from + i].Character == c).Any(x => !x))
                                {
                                    withinSingleLineComment = true;
                                }
                                else if (!withinSingleLineComment && currentIndex + startStr.Length <= line.Length &&
                                         startStr.Select((c, i) => line[from + i].Character == c).Any(x => !x))
                                {
                                    commentStartLine = currentLine;
                                    commentStartIndex = currentIndex;
                                }

                                inComment = commentStartLine < currentLine ||
                                            commentStartLine == currentLine &&
                                             commentStartIndex <= currentIndex;

                                currentGlyph.IsMultiLineComment = inComment;
                                currentGlyph.IsComment = withinSingleLineComment;

                                string endStr = _languageDefinition.CommentEndIdentifier;
                                if (currentIndex + 1 >= endStr.Length &&
                                    endStr.Select((c, i) => line[from + 1 - endStr.Length + i].Character == c).Any(x => !x))
                                {
                                    commentStartIndex = endIndex;
                                    commentStartLine = endLine;
                                }
                            }
                        }

                        currentGlyph.IsPreprocessor = withinPreproc;
                        line[currentIndex] = currentGlyph;

                        currentIndex++;
                        if (currentIndex >= line.Length)
                        {
                            currentIndex = 0;
                            ++currentLine;
                        }
                    }
                    else
                    {
                        currentIndex = 0;
                        ++currentLine;
                    }
                }

                _isCheckComments = false;
            }

            if (_colorRangeMin < _colorRangeMax)
            {
                int increment = _languageDefinition.CanTokenize() ? 10 : 10000;
                int to = Math.Min(_colorRangeMin + increment, _colorRangeMax);
                ColorizeRange(_colorRangeMin, to);
                _colorRangeMin = to;

                if (_colorRangeMax == _colorRangeMin)
                {
                    _colorRangeMin = int.MaxValue;
                    _colorRangeMax = 0;
                }
            }
        }

        private float TextDistanceToLineStart(Coordinate aFrom)
        {
            GlyphLine line = _lines[aFrom.Line];
            var distance = 0.0f;
            float spaceSize = ImGuiNET.ImGui.GetFont().CalcTextSizeA(ImGuiNET.ImGui.GetFontSize(), float.MaxValue, -1.0f, " ").X;
            int colIndex = GetCharacterIndex(aFrom);
            for (var it = 0; it < line.Length && it < colIndex;)
            {
                if (line[it].Character == '\t')
                {
                    distance = (float)(1.0f + Math.Floor((1.0f + distance) / (TabSize * spaceSize))) * (TabSize * spaceSize);
                    ++it;
                }
                else
                {
                    distance += ImGuiNET.ImGui.GetFont().CalcTextSizeA(ImGuiNET.ImGui.GetFontSize(), float.MaxValue, -1.0f, $"{line[it++].Character}").X;
                }
            }

            return distance;
        }

        internal void EnsureCursorVisible()
        {
            if (!_withinRender)
            {
                _scrollToCursor = true;
                return;
            }

            EnsureCoordinateVisible(GetCursorPosition());
        }

        public void EnsureSelectionVisible()
        {
            if (!_withinRender)
            {
                _scrollToSelection = true;
                return;
            }

            EnsureCoordinateVisible(mState.SelectionStart);
        }

        private void EnsureCoordinateVisible(Coordinate aCoordinate)
        {
            float scrollX = ImGuiNET.ImGui.GetScrollX();
            float scrollY = ImGuiNET.ImGui.GetScrollY();

            float height = ImGuiNET.ImGui.GetWindowHeight();
            float width = ImGuiNET.ImGui.GetWindowWidth();

            int top = 1 + (int)Math.Ceiling(scrollY / _characterAdvance.Y);
            var bottom = (int)Math.Ceiling((scrollY + height) / _characterAdvance.Y);

            var left = (int)Math.Ceiling(scrollX / _characterAdvance.X);
            var right = (int)Math.Ceiling((scrollX + width) / _characterAdvance.X);

            Coordinate pos = aCoordinate;
            float len = TextDistanceToLineStart(pos);

            if (pos.Line < top)
                ImGuiNET.ImGui.SetScrollY(Math.Max(0.0f, (pos.Line - 1) * _characterAdvance.Y));
            if (pos.Line > bottom - 4)
                ImGuiNET.ImGui.SetScrollY(Math.Max(0.0f, (pos.Line + 4) * _characterAdvance.Y - height));
            if (len + _textStart < left + 4)
                ImGuiNET.ImGui.SetScrollX(Math.Max(0.0f, len + _textStart - 4));
            if (len + _textStart > right - 4)
                ImGuiNET.ImGui.SetScrollX(Math.Max(0.0f, len + _textStart + 4 - width));
        }

        private int GetPageSize()
        {
            float height = ImGuiNET.ImGui.GetWindowHeight() - 20.0f;
            return (int)Math.Floor(height / _characterAdvance.Y);
        }
    }
}
