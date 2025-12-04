using System;
using System.IO;
using ImGui.Forms.Factories;
using ImGui.Forms.Models;
using ImGuiNET;

namespace ImGui.Forms.Resources;

/// <summary>
/// Represents a single font in ImGui.Forms.
/// </summary>
public class FontResource : IDisposable
{
    /// <summary>
    /// The data of the font.
    /// </summary>
    public FontData Data { get; }

    internal FontResource(FontData data)
    {
        Data = data;
    }

    public void Dispose() => Dispose(true);

    protected virtual void Dispose(bool disposing)
    {
        if (string.IsNullOrEmpty(Data.Metadata.Path))
            return;

        if (File.Exists(Data.Metadata.Path))
            File.Delete(Data.Metadata.Path);
    }

    public FontResource Clone(int size)
    {
        return new FontResource(Clone(Data, size, null));
    }

    public FontResource Clone(int size, string characterSet)
    {
        return new FontResource(Clone(Data, size, characterSet));
    }

    private FontData Clone(FontData data, int size, string? characterSet)
    {
        FontData? fallback = null;
        if (data.Fallback != null)
            fallback = Clone(data.Fallback, size, characterSet);

        var metaData = new FontMetaData(data.Metadata.Name, data.Metadata.Path,
            characterSet == null ? data.Metadata.GlyphRanges : FontGlyphRange.None, characterSet ?? string.Empty,
            data.Metadata.IsTemporary);

        return new FontData(metaData, size, fallback);
    }

    /// <summary>
    /// Gets the line height of this <see cref="FontResource"/>.
    /// </summary>
    /// <returns>The line height of this <see cref="FontResource"/>.</returns>
    public float GetLineHeight() =>
        TextMeasurer.GetCurrentLineHeight(this);

    /// <summary>
    /// Gets the width of a single line with this <see cref="FontResource"/>.
    /// </summary>
    /// <param name="text">The text to measure the width for.</param>
    /// <returns>The width of <paramref name="text"/> with this <see cref="FontResource"/>.</returns>
    public float GetLineWidth(string text) =>
        TextMeasurer.GetCurrentLineWidth(text, this);

    /// <summary>
    /// Gets the native pointer of the font for usage in Dear ImGui.
    /// </summary>
    /// <returns>Native pointer of the font, otherwise null.</returns>
    public ImFontPtr? GetPointer() => FontFactory.GetPointer(Data);
}

public record FontMetaData
{
    /// <summary>
    /// The name of the font.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The physical path to the font.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Determines if the font file has to be deleted at disposal.
    /// </summary>
    internal bool IsTemporary { get; }

    /// <summary>
    /// Supported glyphs.
    /// </summary>
    public FontGlyphRange GlyphRanges { get; }

    /// <summary>
    /// Additional characters to be represented by this font.
    /// </summary>
    public string AdditionalCharacters { get; }

    internal FontMetaData(string name, string path, FontGlyphRange glyphRanges, string additionalCharacters = "", bool temporary = false)
    {
        Name = name;
        Path = path;
        IsTemporary = temporary;
        GlyphRanges = glyphRanges;
        AdditionalCharacters = additionalCharacters;
    }
}

public record FontData
{
    /// <summary>
    /// The metadata for this font.
    /// </summary>
    public FontMetaData Metadata { get; }

    /// <summary>
    /// The size of this font instance.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// The fallback font to look for missing glyphs.
    /// </summary>
    public FontData? Fallback { get; }

    internal FontData(FontMetaData metadata, int size, FontData? fallback = null)
    {
        Metadata = metadata;
        Size = size;
        Fallback = fallback;
    }
}