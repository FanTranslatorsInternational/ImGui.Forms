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

    private static unsafe byte* GetClipboardText_Wrap(nint _) => SDL.GetClipboardText();

    private static unsafe bool SetClipboardText_Wrap(nint _, byte* textPtr) => SDL.SetClipboardText(textPtr);

    public unsafe delegate byte* ImGui_GetClipboardText(nint context);
    public unsafe delegate bool ImGui_SetClipboardText(nint context, byte* textPtr);

    public static unsafe ImGui_GetClipboardText GetClipboardText = GetClipboardText_Wrap;
    public static unsafe ImGui_SetClipboardText SetClipboardText = SetClipboardText_Wrap;
}