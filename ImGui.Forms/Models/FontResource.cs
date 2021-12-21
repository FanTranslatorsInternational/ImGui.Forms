using System.Reflection;
using ImGuiNET;

namespace ImGui.Forms.Models
{
    public class FontResource
    {
        private ImFontPtr _ptr;

        private readonly string _path;

        private readonly Assembly _assembly;
        private readonly string _resourceName;

        public int Size { get; }

        public FontResource(string path, int size)
        {
            _path = path;
            Size = size;
        }

        public FontResource(Assembly assembly, string resourceName, int size)
        {
            _assembly = assembly;
            _resourceName = resourceName;
            Size = size;
        }

        public static explicit operator ImFontPtr(FontResource fr) => fr.GetPointer();

        private ImFontPtr GetPointer()
        {
            if (IsLoaded())
                return _ptr;

            if (!string.IsNullOrEmpty(_path))
                return _ptr = Application.Instance?.FontFactory.LoadFont(_path, Size) ?? null;

            return _ptr = Application.Instance?.FontFactory.LoadFont(_assembly, _resourceName, Size) ?? null;
        }

        private unsafe bool IsLoaded()
        {
            return (int)_ptr.NativePtr != 0;
        }
    }

    public static class Fonts
    {
        private const string ArialPath_ = "imGui.Forms.Resources.Fonts.arial.ttf";
        private const string RobotoPath_ = "imGui.Forms.Resources.Fonts.roboto.ttf";

        public static FontResource Arial(int size)
        {
            return new FontResource(typeof(Fonts).Assembly, ArialPath_, size);
        }

        public static FontResource Roboto(int size)
        {
            return new FontResource(typeof(Fonts).Assembly, RobotoPath_, size);
        }
    }
}
