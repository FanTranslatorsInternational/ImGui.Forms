using System.Numerics;
using Hexa.NET.ImGui;

namespace ImGui.Forms.Resources;

public static class TextMeasurer
{
    /// <summary>
    /// Measure the <paramref name="text"/> with the current font on the stack.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <returns>The measured size of <paramref name="text"/>.</returns>
    public static Vector2 MeasureText(string? text)
    {
        return Hexa.NET.ImGui.ImGui.CalcTextSize(text ?? string.Empty);
    }

    /// <summary>
    /// Measure the <paramref name="text"/> with the current font on the stack.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to measure the text with.</param>
    /// <returns>The measured size of <paramref name="text"/>.</returns>
    public static Vector2 MeasureText(string? text, FontResource? font)
    {
        ImFontPtr? fontPtr = font?.GetPointer();
        if (fontPtr.HasValue)
            Hexa.NET.ImGui.ImGui.PushFont(fontPtr.Value, font!.Data.Size);

        var size = MeasureText(text);

        if (fontPtr.HasValue)
            Hexa.NET.ImGui.ImGui.PopFont();

        return size;
    }

    /// <summary>
    /// Measure the height of a single line for the given font or the current font on the stack.
    /// </summary>
    /// <param name="font">Optional. The font to measure the line height with.</param>
    /// <returns>The measured line height of the given font or the current font on the stack.</returns>
    public static float GetCurrentLineHeight(FontResource? font = null)
    {
        ImFontPtr? fontPtr = font?.GetPointer();
        if (fontPtr.HasValue)
            Hexa.NET.ImGui.ImGui.PushFont(fontPtr.Value, font!.Data.Size);

        var currentFont = Hexa.NET.ImGui.ImGui.GetFont();
        var lineHeight = currentFont.LegacySize;

        if (fontPtr.HasValue)
            Hexa.NET.ImGui.ImGui.PopFont();

        return lineHeight;
    }

    /// <summary>
    /// Measure the width of a single line for <paramref name="text"/> with the given font or the current font on the stack.
    /// </summary>
    /// <param name="text">The line of text to measure.</param>
    /// <param name="font">Optional. The font to measure the width with.</param>
    /// <returns>The measured width for <paramref name="text"/> with the given font or the current font on the stack.</returns>
    public static float GetCurrentLineWidth(string text, FontResource? font = null)
    {
        ImFontPtr? fontPtr = font?.GetPointer();
        if (fontPtr.HasValue)
            Hexa.NET.ImGui.ImGui.PushFont(fontPtr.Value, font!.Data.Size);

        var lineWidth = MeasureText(text).X;

        if (fontPtr.HasValue)
            Hexa.NET.ImGui.ImGui.PopFont();

        return lineWidth;
    }
}