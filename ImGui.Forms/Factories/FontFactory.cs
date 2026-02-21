using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Hexa.NET.ImGui;

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
        if (!_fontCache.TryGetValue(name, out FontMetaData? metadata))
            throw new InvalidOperationException($"Unregistered font {name}.");

        return new FontResource(new FontData(metadata, size, fallbackFont.Data));
    }

    public static FontResource Get(string name, int size)
    {
        // If font with that name is already loaded, return
        if (!_fontCache.TryGetValue(name, out FontMetaData? metadata))
            throw new InvalidOperationException($"Unregistered font {name}.");

        return new FontResource(new FontData(metadata, size));
    }

    internal static ImFontPtr? GetPointer(FontData data)
    {
        if (_fontPointers.TryGetValue(data, out ImFontPtr fontPtr))
            return fontPtr;

        return InitializeFont(data);
    }

    #endregion

    #region Initialization

    private static ImGuiIOPtr _io;

    internal static void Initialize(ImGuiIOPtr io)
    {
        _io = io;

        InitializeFonts();
    }

    private static void InitializeFonts()
    {
        if (_fontRegistrationQueue.Count <= 0)
            return;

        while (_fontRegistrationQueue.Count > 0)
        {
            FontData newFontData = _fontRegistrationQueue.Dequeue();
            _ = InitializeFont(newFontData);
        }
    }

    private static ImFontPtr InitializeFont(FontData data)
    {
        _fontPointers[data] = AddFont(_io, data);

        return _fontPointers[data];
    }

    private static unsafe ImFontPtr AddFont(ImGuiIOPtr io, FontData fontData)
    {
        ImFontConfig* config = new ImFontConfigPtr();

        if (fontData.Fallback != null)
        {
            _ = AddFont(io, fontData.Fallback);

            config = Hexa.NET.ImGui.ImGui.ImFontConfig();
            config->MergeMode = 1;
        }

        return io.Fonts.AddFontFromFileTTF(fontData.Metadata.Path, fontData.Size, config);
    }

    #endregion

    public static void Dispose()
    {
        _fontCache.Clear();
        _fontPointers.Clear();
        _fontRegistrationQueue.Clear();
    }
}