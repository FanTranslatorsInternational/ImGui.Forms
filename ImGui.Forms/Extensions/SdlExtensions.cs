using System;
using Hexa.NET.SDL3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImGui.Forms.Extensions;

static class Sdl2NativeExtensions
{
    public static unsafe void SetWindowIcon(SDLWindowPtr window, Image<Rgba32>? icon)
    {
        if (icon == null)
            return;

        // Prepare surface
        var surfacePtr = SDL.CreateSurface(icon.Width, icon.Height, SDLPixelFormat.Rgba32);

        // Copy data from icon to surface
        if (!SDL.LockSurface(surfacePtr))
            return;

        if (!icon.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> data))
            return;

        int size = icon.Width * icon.Height * 4;

        fixed (Rgba32* imgData = data.Span)
            Buffer.MemoryCopy(imgData, surfacePtr.Pixels, size, size);

        SDL.UnlockSurface(surfacePtr);

        // Set surface as icon
        SDL.SetWindowIcon(window, surfacePtr);
    }

    //private static nint GetClipboardText_Wrap(nint _) => Sdl_GetClipboardText();

    //private static int SetClipboardText_Wrap(nint _, nint textPtr) => Sdl_SetClipboardText(textPtr);

    //public delegate nint ImGui_GetClipboardText(nint context);
    //public delegate int ImGui_SetClipboardText(nint context, nint textPtr);

    //public static ImGui_GetClipboardText GetClipboardText = GetClipboardText_Wrap;
    //public static ImGui_SetClipboardText SetClipboardText = SetClipboardText_Wrap;

    //private delegate nint SDL_GetClipboardText();
    //private delegate int SDL_SetClipboardText(nint text);

    //private static SDL_GetClipboardText Sdl_GetClipboardText = Sdl2Native.LoadFunction<SDL_GetClipboardText>("SDL_GetClipboardText");
    //private static SDL_SetClipboardText Sdl_SetClipboardText = Sdl2Native.LoadFunction<SDL_SetClipboardText>("SDL_SetClipboardText");
}