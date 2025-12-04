using System.Runtime.InteropServices;

namespace ImGui.Forms.Support.Sdl2;
/*Possible formats: https://github.com/project-grove/SDL2.NETCore/blob/223a408d3e5eef5c6dc37949c445d3d25a93e92e/SDL2/SDL_pixels.cs#L43*/

[StructLayout(LayoutKind.Sequential)]
public struct SDL_PixelFormat
{

    public uint format;
    public nint palette;
    public byte BitsPerPixel;
    public byte BytesPerPixel;
    public byte padding_1;
    public byte padding_2;
    public uint Rmask;
    public uint Gmask;
    public uint Bmask;
    public uint Amask;
    public byte Rloss;
    public byte Gloss;
    public byte Bloss;
    public byte Aloss;
    public byte Rshift;
    public byte Gshift;
    public byte Bshift;
    public byte Ashift;
    public int refcount;
    public nint next;

}