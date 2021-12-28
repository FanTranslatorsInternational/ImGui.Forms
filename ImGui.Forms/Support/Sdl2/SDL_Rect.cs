using System.Runtime.InteropServices;

namespace ImGui.Forms.Support.Sdl2
{
    [StructLayout(LayoutKind.Sequential)]
    struct SDL_Rect
    {

        public int x, y;
        public int w, h;

    }
}
