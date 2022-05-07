using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        public void RegisterFromFile(string ttfPath, int size)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Can not register new fonts after application started.");

            // If font not tracked, add it
            if (!_discCache.ContainsKey((ttfPath, size)))
                _discCache[(ttfPath, size)] = new FontResource(ttfPath, size);
        }

        public void RegisterFromResource(Assembly assembly, string resourceName, int size)
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
            _discCache[(fontPath, size)] = new FontResource(fontPath, size, true);
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

        internal void Initialize(ImGuiIOPtr io, ImGuiRenderer controller)
        {
            _io = io;
            _controller = controller;

            // Initialize fonts
            foreach (var discFont in _discCache)
                discFont.Value.Initialize(_io.Fonts.AddFontFromFileTTF(discFont.Key.Item1, discFont.Key.Item2));

            _controller.RecreateFontDeviceTexture();
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
