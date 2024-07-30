using System;
using System.Collections.Generic;
using ImGui.Forms.Support.Veldrid.ImGui;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace ImGui.Forms.Factories
{
    class ImageFactory
    {
        private readonly GraphicsDevice _gd;
        private readonly ImGuiRenderer _controller;

        private readonly IDictionary<Image<Rgba32>, nint> _inputPointers;
        private readonly IDictionary<nint, Image<Rgba32>> _inputPointersReverse;
        private readonly IDictionary<nint, Texture> _ptrTextures;
        private readonly IDictionary<nint, int> _ptrTexturesRefCount;

        private readonly IList<nint> _unloadQueue;

        public ImageFactory(GraphicsDevice gd, ImGuiRenderer controller)
        {
            _gd = gd;
            _controller = controller;
            _inputPointers = new Dictionary<Image<Rgba32>, nint>();
            _inputPointersReverse = new Dictionary<nint, Image<Rgba32>>();
            _ptrTextures = new Dictionary<nint, Texture>();
            _ptrTexturesRefCount = new Dictionary<nint, int>();
            _unloadQueue = new List<nint>();
        }

        public nint LoadImage(Image<Rgba32> img)
        {
            nint ptr;

            if (_inputPointers.ContainsKey(img))
            {
                ptr = _inputPointers[img];
                UpdateImage(ptr);

                _ptrTexturesRefCount[ptr]++;

                return ptr;
            }

            ptr = LoadImageInternal(img);

            _inputPointers[img] = ptr;
            _inputPointersReverse[ptr] = img;
            _ptrTexturesRefCount[ptr] = 1;

            return ptr;
        }

        public void UpdateImage(nint ptr)
        {
            if (!_ptrTextures.ContainsKey(ptr) || !_inputPointersReverse.ContainsKey(ptr))
                return;

            CopyImageData(_ptrTextures[ptr], _inputPointersReverse[ptr]);
        }

        public void UnloadImage(nint ptr)
        {
            if (!_ptrTextures.ContainsKey(ptr))
                return;

            _ptrTexturesRefCount[ptr]--;
            _unloadQueue.Add(ptr);
        }

        private nint LoadImageInternal(Image<Rgba32> image)
        {
            var texture = _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                (uint)image.Width, (uint)image.Height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));

            CopyImageData(texture, image);

            // Add image pointer to cache
            var imgPtr = _controller.GetOrCreateImGuiBinding(_gd.ResourceFactory, texture);
            _ptrTextures[imgPtr] = texture;

            return imgPtr;
        }

        private unsafe void CopyImageData(Texture texture, Image<Rgba32> image)
        {
            if (!image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> data))
                return;

            int size = image.Width * image.Height * 4;

            fixed (Rgba32* imgData = data.Span)
                _gd.UpdateTexture(texture, (nint)imgData, (uint)size, 0, 0, 0, (uint)image.Width, (uint)image.Height, 1, 0, 0);
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
