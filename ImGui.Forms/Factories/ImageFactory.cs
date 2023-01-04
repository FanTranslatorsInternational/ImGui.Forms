using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImGui.Forms.Support.Veldrid.ImGui;
using Veldrid;

namespace ImGui.Forms.Factories
{
    class ImageFactory
    {
        private readonly GraphicsDevice _gd;
        private readonly ImGuiRenderer _controller;

        private readonly IDictionary<object, IntPtr> _inputPointers;
        private readonly IDictionary<IntPtr, object> _inputPointersReverse;
        private readonly IDictionary<IntPtr, Texture> _ptrTextures;
        private readonly IDictionary<IntPtr, int> _ptrTexturesRefCount;

        private readonly IList<IntPtr> _unloadQueue;

        public ImageFactory(GraphicsDevice gd, ImGuiRenderer controller)
        {
            _gd = gd;
            _controller = controller;
            _inputPointers = new Dictionary<object, IntPtr>();
            _inputPointersReverse = new Dictionary<IntPtr, object>();
            _ptrTextures = new Dictionary<IntPtr, Texture>();
            _ptrTexturesRefCount = new Dictionary<IntPtr, int>();
            _unloadQueue = new List<IntPtr>();
        }

        public IntPtr LoadImage(Bitmap img)
        {
            IntPtr ptr;

            if (_inputPointers.ContainsKey(img))
            {
                ptr = _inputPointers[img];

                _ptrTexturesRefCount[ptr]++;

                return ptr;
            }

            ptr = LoadImageInternal(img);

            _inputPointers[img] = ptr;
            _inputPointersReverse[ptr] = img;
            _ptrTexturesRefCount[ptr] = 1;

            return ptr;
        }

        public void UnloadImage(IntPtr ptr)
        {
            if (!_ptrTextures.ContainsKey(ptr))
                return;

            _ptrTexturesRefCount[ptr]--;
            _unloadQueue.Add(ptr);
        }

        private IntPtr LoadImageInternal(Bitmap image)
        {
            var texture = _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                (uint)image.Width, (uint)image.Height, 1, 1, Veldrid.PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled));

            var data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            _gd.UpdateTexture(texture, data.Scan0, (uint)(4 * image.Width * image.Height),
                    0, 0, 0, (uint)image.Width, (uint)image.Height, 1, 0, 0);

            image.UnlockBits(data);

            // Add image pointer to cache
            var imgPtr = _controller.GetOrCreateImGuiBinding(_gd.ResourceFactory, texture);
            _ptrTextures[imgPtr] = texture;

            return imgPtr;
        }

        internal void FreeTextures()
        {
            foreach (var toFree in _unloadQueue)
            {
                // Textures that were marked to unload, but somehow retained/regained references afterwards should not be unloaded
                if (!_ptrTexturesRefCount.ContainsKey(toFree) || _ptrTexturesRefCount[toFree] > 0)
                    continue;

                _controller.RemoveImGuiBinding(_ptrTextures[toFree]);

                _ptrTextures.Remove(toFree);
                _ptrTexturesRefCount.Remove(toFree);

                var obj = _inputPointersReverse[toFree];
                _inputPointersReverse.Remove(toFree);
                _inputPointers.Remove(obj);
            }

            _unloadQueue.Clear();
        }
    }
}
