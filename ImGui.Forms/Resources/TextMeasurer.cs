using ImGuiNET;
using System.Numerics;

namespace ImGui.Forms.Resources
{
    public static class TextMeasurer
    {
        /// <summary>
        /// Measure the <paramref name="text"/> with the current font on the stack.
        /// </summary>
        /// <param name="text">The text to measure.</param>
        /// <param name="withDescent">Calculate height with respect to the font descent.</param>
        /// <returns>The measured size of <paramref name="text"/>.</returns>
        public static Vector2 MeasureText(string text, bool withDescent = false)
        {
            var size = ImGuiNET.ImGui.CalcTextSize(text ?? string.Empty);
            size = size with { Y = size.Y + (withDescent ? -ImGuiNET.ImGui.GetFont().Descent : 0) };

            return size;
        }

        /// <summary>
        /// Measure the <paramref name="text"/> with the current font on the stack.
        /// </summary>
        /// <param name="text">The text to measure.</param>
        /// <param name="font">The font to measure the text with.</param>
        /// <param name="withDescent">Calculate height with respect to the font descent.</param>
        /// <returns>The measured size of <paramref name="text"/>.</returns>
        public static Vector2 MeasureText(string text, FontResource font, bool withDescent = false)
        {
            ImFontPtr? fontPtr = font?.GetPointer();
            if (fontPtr.HasValue)
                ImGuiNET.ImGui.PushFont(fontPtr.Value);

            var size = MeasureText(text, withDescent);

            if (fontPtr.HasValue)
                ImGuiNET.ImGui.PopFont();

            return size;
        }

        /// <summary>
        /// Measure the height of a single line for the given font or the current font on the stack.
        /// </summary>
        /// <param name="font">Optional. The font to measure the line height with.</param>
        /// <param name="withDescent">Calculate height with respect to the font descent.</param>
        /// <returns>The measured line height of the given font or the current font on the stack.</returns>
        public static float GetCurrentLineHeight(FontResource font = null, bool withDescent = false)
        {
            ImFontPtr? fontPtr = font?.GetPointer();
            if (fontPtr.HasValue)
                ImGuiNET.ImGui.PushFont(fontPtr.Value);

            var currentFont = ImGuiNET.ImGui.GetFont();
            var lineHeight = currentFont.Ascent + (withDescent ? -currentFont.Descent : 0);

            if (fontPtr.HasValue)
                ImGuiNET.ImGui.PopFont();

            return lineHeight;
        }

        /// <summary>
        /// Measure the width of a single line for <paramref name="text"/> with the given font or the current font on the stack.
        /// </summary>
        /// <param name="text">The line of text to measure.</param>
        /// <param name="font">Optional. The font to measure the width with.</param>
        /// <returns>The measured width for <paramref name="text"/> with the given font or the current font on the stack.</returns>
        public static float GetCurrentLineWidth(string text, FontResource font = null)
        {
            ImFontPtr? fontPtr = font?.GetPointer();
            if (fontPtr.HasValue)
                ImGuiNET.ImGui.PushFont(fontPtr.Value);

            var lineWidth = MeasureText(text).X;

            if (fontPtr.HasValue)
                ImGuiNET.ImGui.PopFont();

            return lineWidth;
        }
    }
}
