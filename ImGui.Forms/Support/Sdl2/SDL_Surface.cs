﻿using System;
using System.Runtime.InteropServices;

namespace ImGui.Forms.Support.Sdl2
{
    [StructLayout(LayoutKind.Sequential)]
    struct SDL_Surface
    {

        public uint flags;               /**< Read-only */
        public nint format;    /**< Read-only */
        public int w, h;                   /**< Read-only */
        public int pitch;                  /**< Read-only */
        public nint pixels;               /**< Read-write */

        /** Application data associated with the surface */
        public nint userdata;             /**< Read-write */

        /** information needed for surfaces requiring locks */
        public int locked;                 /**< Read-only */
        public nint lock_data;            /**< Read-only */

        /** clipping information */
        public SDL_Rect clip_rect;         /**< Read-only */

        /** info for fast blit mapping to other surfaces */
        // struct SDL_BlitMap map;    /**< Private */
        SDL_BlitMap map;

        /** Reference count -- used when freeing surface */
        int refcount;               /**< Read-mostly */

    }
}
