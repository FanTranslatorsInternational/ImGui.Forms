using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ImGui.Forms.Support.Sdl2;
using Veldrid.Sdl2;

namespace ImGui.Forms.Extensions
{
    static class Sdl2NativeExtensions
    {
        public static void SetWindowIcon(IntPtr window, Bitmap icon)
        {
            // Prepare surface
            var surfacePtr = CreateRgbSurfaceDelegate(0, icon.Width, icon.Height, 32, 0xFF000000, 0xFF0000, 0xFF00, 0xFF);

            // Copy data from icon to surface
            if (LockSurfaceDelegate(surfacePtr) != 0)
                return;

            var data = icon.LockBits(new Rectangle(0, 0, icon.Width, icon.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var surface = Marshal.PtrToStructure<SDL_Surface>(surfacePtr);

            unsafe
            {
                var size = icon.Width * icon.Height * 4;
                Buffer.MemoryCopy((void*)data.Scan0, (void*)surface.pixels, size, size);
            }

            UnlockSurfaceDelegate(surfacePtr);
            icon.UnlockBits(data);

            // Set surface as icon
            SetWindowIconDelegate(window, ref surface);
        }

        private delegate IntPtr SDL_CreateRGBSurface(uint flags, int width, int height, int depth, uint rmask, uint gmask, uint bmask, uint amask);
        private delegate int SDL_LockSurface(IntPtr surface);
        private delegate void SDL_UnlockSurface(IntPtr surface);
        private delegate void SDL_SetWindowIcon(IntPtr window, ref SDL_Surface icon);

        private static SDL_CreateRGBSurface CreateRgbSurfaceDelegate = Sdl2Native.LoadFunction<SDL_CreateRGBSurface>("SDL_CreateRGBSurface");
        private static SDL_LockSurface LockSurfaceDelegate = Sdl2Native.LoadFunction<SDL_LockSurface>("SDL_LockSurface");
        private static SDL_UnlockSurface UnlockSurfaceDelegate = Sdl2Native.LoadFunction<SDL_UnlockSurface>("SDL_UnlockSurface");
        private static SDL_SetWindowIcon SetWindowIconDelegate = Sdl2Native.LoadFunction<SDL_SetWindowIcon>("SDL_SetWindowIcon");
    }
}
