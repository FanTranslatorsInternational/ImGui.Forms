using System;
using System.IO;
using System.Numerics;
using ImGuiNET;

namespace ImGui.Forms.Resources
{
    /// <summary>
    /// Represents a single font in ImGui.Forms.
    /// </summary>
    /// <remarks>To load built-in fonts, see <see cref="FontResources"/>.</remarks>
    public class FontResource : IDisposable
    {
        private ImFontPtr _ptr;

        private readonly string _path;
        private readonly bool _temporary;

        /// <summary>
        /// The size of the font.
        /// </summary>
        public int Size { get; }

        internal FontResource(string path, int size, bool temporary = false)
        {
            _path = path;
            Size = size;
            _temporary = temporary;
        }

        internal void Initialize(ImFontPtr ptr)
        {
            _ptr = ptr;
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            _ptr = IntPtr.Zero;

            if (!_temporary)
                return;

            if (File.Exists(_path))
                File.Delete(_path);
        }

        /// <summary>
        /// Measure the <paramref name="text"/> with the current font on the stack.
        /// </summary>
        /// <param name="text">The text to measure.</param>
        /// <param name="withDescent">Calculate height with respect to the font descent.</param>
        /// <returns>The measured size of <paramref name="text"/>.</returns>
        public static Vector2 MeasureText(string text, bool withDescent = false)
        {
            var size = ImGuiNET.ImGui.CalcTextSize(text ?? string.Empty);
            size = new Vector2(size.X, size.Y + (withDescent ? -ImGuiNET.ImGui.GetFont().Descent : 0));

            return size;
        }

        /// <summary>
        /// Measure the height of a single line for the given font or the current font on the stack.
        /// </summary>
        /// <param name="font">Optional. The font to measure the line height with.</param>
        /// <param name="withDescent">Calculate height with respect to the font descent.</param>
        /// <returns>The measured line height of the given font or the current font on the stack.</returns>
        public static int GetCurrentLineHeight(FontResource font = null, bool withDescent = false)
        {
            if (font != null)
                ImGuiNET.ImGui.PushFont((ImFontPtr)font);

            var currentFont = ImGuiNET.ImGui.GetFont();
            var lineHeight = (int)Math.Ceiling(currentFont.Ascent+ (withDescent ? -currentFont.Descent : 0));

            if (font != null)
                ImGuiNET.ImGui.PopFont();

            return lineHeight;
        }

        /// <summary>
        /// Measure the width of a single line for <paramref name="text"/> with the given font or the current font on the stack.
        /// </summary>
        /// <param name="text">The line of text to measure.</param>
        /// <param name="font">Optional. The font to measure the width with.</param>
        /// <returns>The measured width for <paramref name="text"/> with the given font or the current font on the stack.</returns>
        public static int GetCurrentLineWidth(string text, FontResource font = null)
        {
            if (font != null)
                ImGuiNET.ImGui.PushFont((ImFontPtr)font);

            var lineWidth = (int)Math.Ceiling(MeasureText(text).X);

            if (font != null)
                ImGuiNET.ImGui.PopFont();

            return lineWidth;
        }

        /// <summary>
        /// Gets the line height of this <see cref="FontResource"/>.
        /// </summary>
        /// <returns>The line height of this <see cref="FontResource"/>.</returns>
        public int GetLineHeight()
        {
            return GetCurrentLineHeight(this);
        }

        /// <summary>
        /// Gets the width of a single line with this <see cref="FontResource"/>.
        /// </summary>
        /// <param name="text">The text to measure the width for.</param>
        /// <returns>The width of <paramref name="text"/> with this <see cref="FontResource"/>.</returns>
        public int GetLineWidth(string text)
        {
            return GetCurrentLineWidth(text, this);
        }

        public static explicit operator ImFontPtr(FontResource fr) => fr.GetPointer();

        private ImFontPtr GetPointer()
        {
            if (!IsLoaded())
                throw new InvalidOperationException("Font was not initialized yet.");

            return _ptr;
        }

        //private ImFontPtr GetPointer()
        //{
        //    if (IsLoaded())
        //        return _ptr;

        //    if (!string.IsNullOrEmpty(_path))
        //        return _ptr = Application.Instance?.FontFactory.LoadFont(_path, Size) ?? null;

        //    return _ptr = Application.Instance?.FontFactory.LoadFont(_assembly, _resourceName, Size) ?? null;
        //}

        private unsafe bool IsLoaded()
        {
            return (int)_ptr.NativePtr != 0;
        }
    }
}
