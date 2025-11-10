using System;
using System.Runtime.InteropServices;

namespace ImGui.Forms.Support.Veldrid.Sdl2
{
    internal static class Utilities
    {
        public static unsafe string? GetString(byte* stringStart)
        {
            if (stringStart == null)
                return null;

            return Marshal.PtrToStringUTF8((IntPtr)stringStart);
        }
    }
}
