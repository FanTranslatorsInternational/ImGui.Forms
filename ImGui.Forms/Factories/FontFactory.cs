using ImGui.Forms.Models;
using ImGui.Forms.Providers;
using ImGui.Forms.Resources;
using ImGui.Forms.Support.Veldrid.ImGui;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ImGui.Forms.Factories;

public static class FontFactory
{
    private const string DefaultFontName_ = "ProggyClean";
    private const string DefaultFontResourceName_ = "ProggyClean.ttf";

    private static readonly Dictionary<string, FontMetaData> _fontCache = new();
    private static readonly Dictionary<FontData, ImFontPtr> _fontPointers = new();

    private static readonly Queue<FontData> _fontRegistrationQueue = new();

    static FontFactory()
    {
        RegisterFromResource(DefaultFontName_, Assembly.GetExecutingAssembly(), DefaultFontResourceName_, FontGlyphRange.Latin);
    }

    #region Registration

    public static void RegisterFromFile(string name, string ttfPath, FontGlyphRange glyphRanges = FontGlyphRange.All, string additionalCharacters = "")
    {
        // If font with that name is already loaded, return
        if (_fontCache.ContainsKey(name))
            return;

        // Add file font to cache
        _fontCache[name] = new FontMetaData(name, ttfPath, glyphRanges, additionalCharacters);
    }

    public static void RegisterFromResource(string name, string resourceName, FontGlyphRange glyphRanges = FontGlyphRange.All, string additionalCharacters = "")
    {
        RegisterFromResource(name, Assembly.GetCallingAssembly(), resourceName, glyphRanges, additionalCharacters);
    }

    public static void RegisterFromResource(string name, Assembly assembly, string resourceName, FontGlyphRange glyphRanges = FontGlyphRange.All, string additionalCharacters = "")
    {
        // If font with that name is already loaded, return
        if (_fontCache.ContainsKey(name))
            return;

        // Load font from resource
        var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream == null)
            return;

        var tempFile = Path.GetTempFileName();
        var tempFileStream = File.OpenWrite(tempFile);
        resourceStream.CopyTo(tempFileStream);
        tempFileStream.Close();

        // Add resource font to cache
        _fontCache[name] = new FontMetaData(name, tempFile, glyphRanges, additionalCharacters, true);
    }

    #endregion

    #region Get fonts

    public static FontResource GetDefault(int size, FontResource fallbackFont)
    {
        return Get(DefaultFontName_, size, fallbackFont);
    }

    public static FontResource GetDefault(int size)
    {
        return Get(DefaultFontName_, size);
    }

    public static FontResource Get(string name, int size, FontResource fallbackFont)
    {
        // If font with that name is already loaded, return
        if (!_fontCache.TryGetValue(name, out FontMetaData metadata))
            throw new InvalidOperationException($"Unregistered font {name}.");

        return new FontResource(new FontData(metadata, size, fallbackFont.Data));
    }

    public static FontResource Get(string name, int size)
    {
        // If font with that name is already loaded, return
        if (!_fontCache.TryGetValue(name, out FontMetaData metadata))
            throw new InvalidOperationException($"Unregistered font {name}.");

        return new FontResource(new FontData(metadata, size));
    }

    internal static ImFontPtr? GetPointer(FontData data)
    {
        if (_fontPointers.TryGetValue(data, out ImFontPtr fontPtr))
            return fontPtr;

        if (_controller != null)
            return InitializeFont(data, true);

        if (!_fontRegistrationQueue.Contains(data))
            _fontRegistrationQueue.Enqueue(data);

        return null;
    }

    #endregion

    #region Initialization

    private static ImGuiIOPtr _io;
    private static ImGuiRenderer _controller;

    internal static void Initialize(ImGuiIOPtr io, ImGuiRenderer controller)
    {
        _io = io;
        _controller = controller;

        InitializeFonts();
    }

    private static void InitializeFonts()
    {
        if (_fontRegistrationQueue.Count <= 0)
            return;

        while (_fontRegistrationQueue.Count > 0)
        {
            FontData newFontData = _fontRegistrationQueue.Dequeue();
            _ = InitializeFont(newFontData, false);
        }

        _controller.RecreateFontDeviceTexture();
    }

    private static ImFontPtr InitializeFont(FontData data, bool recreateDevice)
    {
        _fontPointers[data] = AddFont(_io, data);

        if (recreateDevice)
            _controller.RecreateFontDeviceTexture();

        return _fontPointers[data];
    }

    private static unsafe ImFontPtr AddFont(ImGuiIOPtr io, FontData fontData)
    {
        ImFontConfig* config = new ImFontConfigPtr();

        if (fontData.Fallback != null)
        {
            _ = AddFont(io, fontData.Fallback);

            config = ImGuiNative.ImFontConfig_ImFontConfig();
            config->MergeMode = 1;
        }

        ImVector ranges = GetGlyphRanges(fontData.Metadata.GlyphRanges, fontData.Metadata.AdditionalCharacters);
        return io.Fonts.AddFontFromFileTTF(fontData.Metadata.Path, fontData.Size, config, ranges.Data);
    }

    private static unsafe ImVector GetGlyphRanges(FontGlyphRange rangeFlags, string additionalCharacters)
    {
        var builder = new ImFontGlyphRangesBuilderPtr(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());
        builder.Clear();

        if (rangeFlags.HasFlag(FontGlyphRange.Latin))
            builder.AddRanges(GlyphRangeProvider.GetLatinRange());
        if (rangeFlags.HasFlag(FontGlyphRange.Cyrillic))
            builder.AddRanges(GlyphRangeProvider.GetCyrillicRange());
        if (rangeFlags.HasFlag(FontGlyphRange.ChineseJapanese))
            builder.AddRanges(GlyphRangeProvider.GetCjRange());
        if (rangeFlags.HasFlag(FontGlyphRange.Korean))
            builder.AddRanges(GlyphRangeProvider.GetKoreanRange());
        if (rangeFlags.HasFlag(FontGlyphRange.Greek))
            builder.AddRanges(GlyphRangeProvider.GetGreekRange());
        if (rangeFlags.HasFlag(FontGlyphRange.Thai))
            builder.AddRanges(GlyphRangeProvider.GetThaiRange());
        if (rangeFlags.HasFlag(FontGlyphRange.Vietnamese))
            builder.AddRanges(GlyphRangeProvider.GetVietnameseRange());
        if (rangeFlags.HasFlag(FontGlyphRange.Symbols))
            builder.AddRanges(GlyphRangeProvider.GetSymbolRange());

        if (!string.IsNullOrEmpty(additionalCharacters))
            builder.AddText(additionalCharacters);

        builder.BuildRanges(out ImVector ranges);

        return ranges;
    }

    #endregion

    public static void Dispose()
    {
        _fontCache.Clear();
        _fontPointers.Clear();
        _fontRegistrationQueue.Clear();
    }
}