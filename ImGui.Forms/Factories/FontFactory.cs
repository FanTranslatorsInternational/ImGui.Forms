using System;
using System.Collections.Generic;
using System.Reflection;
using ImGui.Forms.Support.Veldrid.ImGui;
using ImGuiNET;

namespace ImGui.Forms.Factories
{
    public class FontFactory
    {
        private readonly ImGuiIOPtr _io;
        private readonly ImGuiRenderer _controller;

        private readonly IDictionary<(string, int), ImFontPtr> _discFonts;
        private readonly IDictionary<(Assembly, string, int), ImFontPtr> _resourceFonts;

        public FontFactory(ImGuiIOPtr io, ImGuiRenderer controller)
        {
            _io = io;
            _controller = controller;

            _discFonts = new Dictionary<(string, int), ImFontPtr>();
            _resourceFonts = new Dictionary<(Assembly, string, int), ImFontPtr>();
        }

        public ImFontPtr LoadFont(string ttfPath, int size)
        {
            // If font already loaded, do nothing
            if (_discFonts.ContainsKey((ttfPath, size)))
                return _discFonts[(ttfPath, size)];

            // Otherwise, load it into the renderer
            _discFonts[(ttfPath, size)] = _io.Fonts.AddFontFromFileTTF(ttfPath, size);
            _io.Fonts.Build();
            
            _controller.RecreateFontDeviceTexture();

            return _discFonts[(ttfPath, size)];
        }

        public unsafe ImFontPtr LoadFont(Assembly assembly, string resourceName, int size)
        {
            // If font already loaded, do nothing
            if (_resourceFonts.ContainsKey((assembly, resourceName, size)))
                return _resourceFonts[(assembly, resourceName, size)];

            // Otherwise, load it into the renderer
            var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
                return null;

            var data = new byte[resourceStream.Length];
            resourceStream.Read(data);

            fixed (byte* ptr = data)
            {
                _resourceFonts[(assembly, resourceName, size)] = _io.Fonts.AddFontFromMemoryTTF((IntPtr)ptr, data.Length, size);
                _io.Fonts.Build();

                _controller.RecreateFontDeviceTexture();

                return _resourceFonts[(assembly, resourceName, size)];
            }
        }
    }
}
