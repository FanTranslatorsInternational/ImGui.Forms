using ImGui.Forms.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hexa.NET.ImGui;

namespace ImGui.Forms.Factories;

public static class FontFactory
{
    private const string DefaultFontName = "ProggyClean";
    private const string DefaultFontResourceName = "ProggyClean.ttf";

    private static readonly Dictionary<string, FontMetaData> FontCache = [];
    private static readonly Dictionary<FontData, ImFontPtr> FontPointers = [];

    static FontFactory()
    {
        RegisterFromResource(DefaultFontName, Assembly.GetExecutingAssembly(), DefaultFontResourceName);
    }

    #region Registration

    public static void RegisterFromFile(string name, string ttfPath)
    {
        // If font with that name is already loaded, return
        if (FontCache.ContainsKey(name))
            return;

        // Add file font to cache
        FontCache[name] = new FontMetaData(name, ttfPath);
    }

    public static void RegisterFromResource(string name, string resourceName)
    {
        RegisterFromResource(name, Assembly.GetCallingAssembly(), resourceName);
    }

    public static void RegisterFromResource(string name, Assembly assembly, string resourceName)
    {
        // If font with that name is already loaded, return
        if (FontCache.ContainsKey(name))
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
        FontCache[name] = new FontMetaData(name, tempFile, true);
    }

    #endregion

    #region Get fonts

    public static FontResource GetDefault(int size, FontResource fallbackFont)
    {
        return Get(DefaultFontName, size, fallbackFont);
    }

    public static FontResource GetDefault(int size)
    {
        return Get(DefaultFontName, size);
    }

    public static FontResource Get(string name, int size, FontResource fallbackFont)
    {
        // If font with that name is already loaded, return
        if (!FontCache.TryGetValue(name, out FontMetaData? metadata))
            throw new InvalidOperationException($"Unregistered font {name}.");

        return new FontResource(new FontData(metadata, size, fallbackFont.Data));
    }

    public static FontResource Get(string name, int size)
    {
        // If font with that name is already loaded, return
        if (!FontCache.TryGetValue(name, out FontMetaData? metadata))
            throw new InvalidOperationException($"Unregistered font {name}.");

        return new FontResource(new FontData(metadata, size));
    }

    internal static ImFontPtr? GetPointer(FontData data)
    {
        if (FontPointers.TryGetValue(data, out ImFontPtr fontPtr))
            return fontPtr;

        return InitializeFont(data);
    }

    #endregion

    #region Initialization

    private static ImGuiIOPtr _io;

    internal static void Initialize(ImGuiIOPtr io)
    {
        _io = io;
    }

    private static unsafe ImFontPtr InitializeFont(FontData data)
    {
        ImFontPtr baseFont = _io.Fonts.AddFontFromFileTTF(data.Metadata.Path, data.Size, new ImFontConfigPtr());

        ImFontConfig* config = Hexa.NET.ImGui.ImGui.ImFontConfig();
        config->MergeMode = 1;

        foreach (FontData fallback in CollectFallbackFonts(data).Reverse())
            _ = _io.Fonts.AddFontFromFileTTF(fallback.Metadata.Path, fallback.Size, config);

        return FontPointers[data] = baseFont;
    }

    private static IEnumerable<FontData> CollectFallbackFonts(FontData fontData)
    {
        FontData? fallback = fontData.Fallback;

        while (fallback is not null)
        {
            yield return fallback;
            fallback = fallback.Fallback;
        }
    }

    #endregion

    public static void Dispose()
    {
        FontCache.Clear();
        FontPointers.Clear();
    }
}