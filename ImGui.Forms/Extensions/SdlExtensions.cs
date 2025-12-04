using System;
using System.Runtime.InteropServices;
using ImGui.Forms.Support.Sdl2;
using Veldrid.Sdl2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImGui.Forms.Extensions;

static class Sdl2NativeExtensions
{
    public static unsafe void SetWindowIcon(nint window, Image<Rgba32> icon)
    {
        // Prepare surface
        var surfacePtr = CreateRgbSurfaceDelegate(0, icon.Width, icon.Height, 32, 0xFF, 0xFF00, 0xFF0000, 0xFF000000);

        // Copy data from icon to surface
        if (LockSurfaceDelegate(surfacePtr) != 0)
            return;

        if (!icon.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> data))
            return;

        var surface = Marshal.PtrToStructure<SDL_Surface>(surfacePtr);

        int size = icon.Width * icon.Height * 4;

        fixed (Rgba32* imgData = data.Span)
            Buffer.MemoryCopy(imgData, (void*)surface.pixels, size, size);

        UnlockSurfaceDelegate(surfacePtr);

        // Set surface as icon
        SetWindowIconDelegate(window, ref surface);
    }

    private static nint GetClipboardText_Wrap(nint _) => Sdl_GetClipboardText();

    private static int SetClipboardText_Wrap(nint _, nint textPtr) => Sdl_SetClipboardText(textPtr);

    public delegate nint ImGui_GetClipboardText(nint context);
    public delegate int ImGui_SetClipboardText(nint context, nint textPtr);

    public static ImGui_GetClipboardText GetClipboardText = GetClipboardText_Wrap;
    public static ImGui_SetClipboardText SetClipboardText = SetClipboardText_Wrap;

    private delegate nint SDL_CreateRGBSurface(uint flags, int width, int height, int depth, uint rmask, uint gmask, uint bmask, uint amask);
    private delegate int SDL_LockSurface(nint surface);
    private delegate void SDL_UnlockSurface(nint surface);
    private delegate void SDL_SetWindowIcon(nint window, ref SDL_Surface icon);
    private delegate nint SDL_GetClipboardText();
    private delegate int SDL_SetClipboardText(nint text);

    private static SDL_CreateRGBSurface CreateRgbSurfaceDelegate = Sdl2Native.LoadFunction<SDL_CreateRGBSurface>("SDL_CreateRGBSurface");
    private static SDL_LockSurface LockSurfaceDelegate = Sdl2Native.LoadFunction<SDL_LockSurface>("SDL_LockSurface");
    private static SDL_UnlockSurface UnlockSurfaceDelegate = Sdl2Native.LoadFunction<SDL_UnlockSurface>("SDL_UnlockSurface");
    private static SDL_SetWindowIcon SetWindowIconDelegate = Sdl2Native.LoadFunction<SDL_SetWindowIcon>("SDL_SetWindowIcon");
    private static SDL_GetClipboardText Sdl_GetClipboardText = Sdl2Native.LoadFunction<SDL_GetClipboardText>("SDL_GetClipboardText");
    private static SDL_SetClipboardText Sdl_SetClipboardText = Sdl2Native.LoadFunction<SDL_SetClipboardText>("SDL_SetClipboardText");
}