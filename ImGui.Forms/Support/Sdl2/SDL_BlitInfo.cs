using System.Runtime.InteropServices;

namespace ImGui.Forms.Support.Sdl2;

[StructLayout(LayoutKind.Sequential)]
unsafe struct SDL_BlitInfo
{
    byte* src;
    int src_w, src_h;
    int src_pitch;
    int src_skip;
    byte* dst;
    int dst_w, dst_h;
    int dst_pitch;
    int dst_skip;
    SDL_PixelFormat* src_fmt;
    SDL_PixelFormat* dst_fmt;
    byte* table;
    int flags;
    uint colorkey;
    byte r, g, b, a;
}