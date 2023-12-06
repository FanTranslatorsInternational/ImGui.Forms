using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGui.Forms.Support.Veldrid.ImGui;
using ImGuiNET;

namespace ImGui.Forms.Factories
{
    public class FontFactory : IDisposable
    {
        private readonly IDictionary<(Assembly, string), string> _resourceCache;
        private readonly IDictionary<(string, int), FontResource> _discCache;

        private ImGuiIOPtr _io;
        private ImGuiRenderer _controller;

        public bool IsInitialized { get; set; }

        internal FontFactory()
        {
            _discCache = new Dictionary<(string, int), FontResource>();
            _resourceCache = new Dictionary<(Assembly, string), string>();
        }

        #region Registration

        public void RegisterFromFile(string ttfPath, int size, FontGlyphRange glyphRanges = FontGlyphRange.All)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Can not register new fonts after application started.");

            // If font not tracked, add it
            if (!_discCache.ContainsKey((ttfPath, size)))
                _discCache[(ttfPath, size)] = new FontResource(ttfPath, size, glyphRanges);
        }

        public void RegisterFromResource(Assembly assembly, string resourceName, int size, FontGlyphRange glyphRanges = FontGlyphRange.All)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Can not register new fonts after application started.");

            // If font is not tracked, add it
            if (!_resourceCache.ContainsKey((assembly, resourceName)))
            {
                var resourceStream = assembly.GetManifestResourceStream(resourceName);
                if (resourceStream == null)
                    return;

                var tempFile = Path.GetTempFileName();
                var tempFileStream = File.OpenWrite(tempFile);
                resourceStream.CopyTo(tempFileStream);
                tempFileStream.Close();

                _resourceCache[(assembly, resourceName)] = tempFile;
            }

            // If resource font with size is already tracked, return
            var fontPath = _resourceCache[(assembly, resourceName)];
            if (_discCache.ContainsKey((fontPath, size)))
                return;

            // Otherwise add it to cache
            _discCache[(fontPath, size)] = new FontResource(fontPath, size, glyphRanges, true);
        }

        #endregion

        #region Get fonts

        public FontResource Get(string ttfPath, int size)
        {
            if (!_discCache.ContainsKey((ttfPath, size)))
                throw new InvalidOperationException("Font was not registered before the application was started.");

            return _discCache[(ttfPath, size)];
        }

        public FontResource Get(Assembly assembly, string resourceName, int size)
        {
            if (!_resourceCache.ContainsKey((assembly, resourceName)))
                throw new InvalidOperationException("Font was not registered before the application was started.");

            var fontPath = _resourceCache[(assembly, resourceName)];
            if (!_discCache.ContainsKey((fontPath, size)))
                throw new InvalidOperationException("Font was not registered before the application was started.");

            return _discCache[(fontPath, size)];
        }

        #endregion

        #region Initialization

        internal unsafe void Initialize(ImGuiIOPtr io, ImGuiRenderer controller)
        {
            _io = io;
            _controller = controller;

            // Initialize default font
            var defaultFont = InitializeDefaultFont();

            // Initialize fonts
            var config = ImGuiNative.ImFontConfig_ImFontConfig();
            config->MergeMode = 1;

            foreach (var discFont in _discCache)
            {
                if (discFont.Value == defaultFont)
                    continue;

                var ranges = GetGlyphRanges(discFont.Value.GlyphRanges);

                var loadedFont = _io.Fonts.AddFontFromFileTTF(discFont.Key.Item1, discFont.Key.Item2, config, ranges.Data);
                discFont.Value.Initialize(loadedFont);
            }

            _controller.RecreateFontDeviceTexture();
        }

        private unsafe FontResource InitializeDefaultFont()
        {
            _io.Fonts.Clear();

            ImFontPtr defaultFontPtr;

            var defaultFont = _discCache.Values.FirstOrDefault(f => f.GlyphRanges.HasFlag(FontGlyphRange.Default));
            if (defaultFont == null)
            {
                defaultFontPtr = _io.Fonts.AddFontDefault().NativePtr;
            }
            else
            {
                defaultFontPtr = _io.Fonts.AddFontFromFileTTF(defaultFont.Path, defaultFont.Size, null, GetGlyphRanges(defaultFont.GlyphRanges).Data);
                defaultFont.Initialize(defaultFontPtr);
            }

            var config = ImGuiNative.ImFontConfig_ImFontConfig();
            config->DstFont = defaultFontPtr;

            return defaultFont;
        }

        private unsafe ImVector GetGlyphRanges(FontGlyphRange rangeFlags)
        {
            var builder = new ImFontGlyphRangesBuilderPtr(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());
            builder.Clear();

            if (rangeFlags.HasFlag(FontGlyphRange.Default))
                builder.AddRanges(_io.Fonts.GetGlyphRangesDefault());
            if (rangeFlags.HasFlag(FontGlyphRange.Cyrillic))
                builder.AddRanges(_io.Fonts.GetGlyphRangesCyrillic());
            if (rangeFlags.HasFlag(FontGlyphRange.Chinese))
                builder.AddRanges(_io.Fonts.GetGlyphRangesChineseFull());
            if (rangeFlags.HasFlag(FontGlyphRange.Japanese))
                builder.AddRanges(_io.Fonts.GetGlyphRangesJapanese());
            if (rangeFlags.HasFlag(FontGlyphRange.Greek))
                builder.AddRanges(_io.Fonts.GetGlyphRangesGreek());
            if (rangeFlags.HasFlag(FontGlyphRange.Korean))
                builder.AddRanges(_io.Fonts.GetGlyphRangesKorean());
            if (rangeFlags.HasFlag(FontGlyphRange.Thai))
                builder.AddRanges(_io.Fonts.GetGlyphRangesThai());
            if (rangeFlags.HasFlag(FontGlyphRange.Vietnamese))
                builder.AddRanges(_io.Fonts.GetGlyphRangesVietnamese());

            builder.BuildRanges(out var ranges);

            return ranges;
        }

        #endregion

        public void Dispose()
        {
            _io = null;
            _controller = null;

            foreach (var discFont in _discCache)
                discFont.Value.Dispose();
        }
    }
}
