using System;
using System.Runtime.InteropServices;

namespace ImGui.Forms.Support.Sdl2
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct SDL_BlitMap
    {
        SDL_Surface* dst;
        int identity;
        nint blit;
        void* data;
        SDL_BlitInfo info;

        /* the version count matches the destination; mismatch indicates
           an invalid mapping */
        uint dst_palette_version;
        uint src_palette_version;
    }
}
