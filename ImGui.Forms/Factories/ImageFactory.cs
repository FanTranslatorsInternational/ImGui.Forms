using Hexa.NET.SDL3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;

namespace ImGui.Forms.Factories;

internal unsafe class ImageFactory(SDLRenderer* renderer)
{
    private readonly object _sync = new();
    private readonly Dictionary<Image<Rgba32>, nint> _inputPointers = [];
    private readonly Dictionary<nint, Image<Rgba32>> _inputPointersReverse = [];
    private readonly Dictionary<nint, SDLTexturePtr> _ptrTextures = [];
    private readonly Dictionary<Image<Rgba32>, int> _imageRefCount = [];
    private readonly Dictionary<nint, bool> _ptrUsedThisFrame = [];
    private readonly List<nint> _unloadQueue = [];

    public void RegisterImage(Image<Rgba32> image)
    {
        lock (_sync)
        {
            _imageRefCount.TryGetValue(image, out var refCount);
            _imageRefCount[image] = refCount + 1;
        }
    }

    public nint GetOrLoadImage(Image<Rgba32> image)
    {
        lock (_sync)
        {
            if (_inputPointers.TryGetValue(image, out var texturePtr) && _ptrTextures.ContainsKey(texturePtr))
            {
                TouchTexture(texturePtr);
                return texturePtr;
            }

            if (_inputPointers.TryGetValue(image, out var stalePtr))
            {
                _inputPointers.Remove(image);
                _inputPointersReverse.Remove(stalePtr);
                _ptrUsedThisFrame.Remove(stalePtr);
            }

            var ptr = Load2DTexture(image);
            _inputPointers[image] = ptr;
            _inputPointersReverse[ptr] = image;
            TouchTexture(ptr);
            return ptr;
        }
    }

    public void UnregisterImage(Image<Rgba32> image)
    {
        lock (_sync)
        {
            if (!_imageRefCount.TryGetValue(image, out var refCount))
                return;

            if (refCount > 1)
            {
                _imageRefCount[image] = refCount - 1;
                return;
            }

            _imageRefCount.Remove(image);
            if (_inputPointers.TryGetValue(image, out var ptr))
                _unloadQueue.Add(ptr);
        }
    }

    public void UpdateImage(nint ptr)
    {
        lock (_sync)
        {
            if (!_ptrTextures.ContainsKey(ptr) || !_inputPointersReverse.TryGetValue(ptr, out Image<Rgba32>? texture))
                return;

            UpdateTexture(_ptrTextures[ptr], texture);
            TouchTexture(ptr);
        }
    }

    private void TouchTexture(nint ptr) => _ptrUsedThisFrame[ptr] = true;

    private nint Load2DTexture(Image<Rgba32> image)
    {
        SDLTexturePtr texture = CreateTexture(image);
        nint imagePtr = (nint)texture.Handle;
        _ptrTextures[imagePtr] = texture;
        return imagePtr;
    }

    private SDLTexturePtr CreateTexture(Image<Rgba32> image)
    {
        SDLTexturePtr texture = SDL.CreateTexture(renderer, SDLPixelFormat.Rgba32, SDLTextureAccess.Streaming, image.Width, image.Height);
        if (texture.Handle == null)
            throw new InvalidOperationException($"Failed to create SDL texture: {SDL.GetErrorS()}");

        SDL.SetTextureScaleMode(texture, SDLScaleMode.Nearest);
        UpdateTexture(texture, image);
        return texture;
    }

    private static unsafe void UpdateTexture(SDLTexturePtr texture, Image<Rgba32> image)
    {
        var copiedImage = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(copiedImage);
        int pitch = image.Width * sizeof(uint);

        fixed (Rgba32* imgData = copiedImage)
        {
            if (!SDL.UpdateTexture(texture, (SDLRect*)0, imgData, pitch))
                throw new InvalidOperationException($"Failed to update SDL texture: {SDL.GetErrorS()}");
        }
    }

    internal void FreeTextures()
    {
        lock (_sync)
        {
            HashSet<nint>? toFree = null;

            foreach (var (ptr, usedThisFrame) in _ptrUsedThisFrame)
            {
                if (usedThisFrame || !_ptrTextures.ContainsKey(ptr))
                    continue;
                toFree ??= [];
                toFree.Add(ptr);
            }

            foreach (var ptr in _unloadQueue)
            {
                if (!_ptrTextures.ContainsKey(ptr))
                    continue;
                toFree ??= [];
                toFree.Add(ptr);
            }

            if (toFree is { Count: > 0 })
            {
                foreach (var ptr in toFree)
                {
                    if (!_ptrTextures.TryGetValue(ptr, out var texture))
                        continue;

                    SDL.DestroyTexture(texture);
                    _ptrTextures.Remove(ptr);
                    _ptrUsedThisFrame.Remove(ptr);

                    if (!_inputPointersReverse.TryGetValue(ptr, out var image))
                        continue;

                    _inputPointersReverse.Remove(ptr);
                    _inputPointers.Remove(image);
                }
            }

            foreach (var ptr in _ptrUsedThisFrame.Keys)
                _ptrUsedThisFrame[ptr] = false;

            _unloadQueue.Clear();
        }
    }

    internal void Dispose()
    {
        lock (_sync)
        {
            foreach (var ptrTexture in _ptrTextures)
                SDL.DestroyTexture(ptrTexture.Value);

            _ptrTextures.Clear();
            _imageRefCount.Clear();
            _ptrUsedThisFrame.Clear();
            _inputPointers.Clear();
            _inputPointersReverse.Clear();
            _unloadQueue.Clear();
        }
    }
}
/*
using Hexa.NET.SDL3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;

namespace ImGui.Forms.Factories;

internal unsafe class ImageFactory(SDLRenderer* renderer)
{
    private readonly object _sync = new();
    private readonly Dictionary<Image<Rgba32>, nint> _inputPointers = [];
    private readonly Dictionary<nint, Image<Rgba32>> _inputPointersReverse = [];
    private readonly Dictionary<nint, SDLTexturePtr> _ptrTextures = [];
    private readonly Dictionary<Image<Rgba32>, int> _imageRefCount = [];
    private readonly Dictionary<nint, bool> _ptrUsedThisFrame = [];
    private readonly List<nint> _unloadQueue = [];

    public void RegisterImage(Image<Rgba32> image)
    {
        lock (_sync)
        {
            _imageRefCount.TryGetValue(image, out var refCount);
            _imageRefCount[image] = refCount + 1;
        }
    }

    public nint GetOrLoadImage(Image<Rgba32> image)
    {
        lock (_sync)
        {
            if (_inputPointers.TryGetValue(image, out var texturePtr) && _ptrTextures.ContainsKey(texturePtr))
            {
                TouchTexture(texturePtr);
                return texturePtr;
            }

            if (_inputPointers.TryGetValue(image, out var stalePtr))
            {
                _inputPointers.Remove(image);
                _inputPointersReverse.Remove(stalePtr);
                _ptrUsedThisFrame.Remove(stalePtr);
            }

            var ptr = Load2DTexture(image);

            _inputPointers[image] = ptr;
            _inputPointersReverse[ptr] = image;
            TouchTexture(ptr);

            return ptr;
        }
    }

    public void UnregisterImage(Image<Rgba32> image)
    {
        lock (_sync)
        {
            if (!_imageRefCount.TryGetValue(image, out var refCount))
                return;

            if (refCount > 1)
            {
                _imageRefCount[image] = refCount - 1;
                return;
            }

            _imageRefCount.Remove(image);

            if (_inputPointers.TryGetValue(image, out var ptr))
                _unloadQueue.Add(ptr);
        }
    }

    public void UpdateImage(nint ptr)
    {
        lock (_sync)
        {
            if (!_ptrTextures.ContainsKey(ptr) || !_inputPointersReverse.TryGetValue(ptr, out Image<Rgba32>? texture))
                return;

            UpdateTexture(_ptrTextures[ptr], texture);
            TouchTexture(ptr);
        }
    }

    private void TouchTexture(nint ptr)
    {
        _ptrUsedThisFrame[ptr] = true;
    }

    private nint Load2DTexture(Image<Rgba32> image)
    {
        SDLTexturePtr texture = CreateTexture(image);
        nint imagePtr = (nint)texture.Handle;
        _ptrTextures[imagePtr] = texture;
        return imagePtr;
    }

    private SDLTexturePtr CreateTexture(Image<Rgba32> image)
    {
        SDLTexturePtr texture = SDL.CreateTexture(renderer, SDLPixelFormat.Rgba32, SDLTextureAccess.Streaming, image.Width, image.Height);
        if (texture.Handle == null)
            throw new InvalidOperationException($"Failed to create SDL texture: {SDL.GetErrorS()}");

        SDL.SetTextureScaleMode(texture, SDLScaleMode.Nearest);
        UpdateTexture(texture, image);
        return texture;
    }

    private static unsafe void UpdateTexture(SDLTexturePtr texture, Image<Rgba32> image)
    {
        var copiedImage = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(copiedImage);
        int pitch = image.Width * sizeof(uint);

        fixed (Rgba32* imgData = copiedImage)
        {
            if (!SDL.UpdateTexture(texture, (SDLRect*)0, imgData, pitch))
                throw new InvalidOperationException($"Failed to update SDL texture: {SDL.GetErrorS()}");
        }
    }

    internal void FreeTextures()
    {
        lock (_sync)
        {
            HashSet<nint>? toFree = null;

            foreach (var (ptr, usedThisFrame) in _ptrUsedThisFrame)
            {
                if (usedThisFrame)
                    continue;

                if (!_ptrTextures.ContainsKey(ptr))
                    continue;

                toFree ??= [];
                toFree.Add(ptr);
            }

            foreach (var ptr in _unloadQueue)
            {
                if (!_ptrTextures.ContainsKey(ptr))
                    continue;

                toFree ??= [];
                toFree.Add(ptr);
            }

            if (toFree is { Count: > 0 })
            {
                foreach (var ptr in toFree)
                {
                    if (!_ptrTextures.TryGetValue(ptr, out var texture))
                        continue;

                    SDL.DestroyTexture(texture);
                    _ptrTextures.Remove(ptr);
                    _ptrUsedThisFrame.Remove(ptr);

                    if (!_inputPointersReverse.TryGetValue(ptr, out var image))
                        continue;

                    _inputPointersReverse.Remove(ptr);
                    _inputPointers.Remove(image);
                }
            }

            foreach (var ptr in _ptrUsedThisFrame.Keys)
                _ptrUsedThisFrame[ptr] = false;

            _unloadQueue.Clear();
        }
    }

    internal void Dispose()
    {
        lock (_sync)
        {
            foreach (var ptrTexture in _ptrTextures)
                SDL.DestroyTexture(ptrTexture.Value);

            _ptrTextures.Clear();
            _imageRefCount.Clear();
            _ptrUsedThisFrame.Clear();
            _inputPointers.Clear();
            _inputPointersReverse.Clear();
            _unloadQueue.Clear();
        }
    }
}
using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;

namespace ImGui.Forms.Factories;

internal class ImageFactory
{
    private readonly object _sync = new();
    private readonly Dictionary<Image<Rgba32>, nint> _inputPointers = [];
    private readonly Dictionary<nint, Image<Rgba32>> _inputPointersReverse = [];
    private readonly Dictionary<nint, int> _ptrTextures = [];
    private readonly Dictionary<Image<Rgba32>, int> _imageRefCount = [];
    private readonly Dictionary<nint, bool> _ptrUsedThisFrame = [];
    private readonly List<nint> _unloadQueue = [];

    public void RegisterImage(Image<Rgba32> image)
    {
        lock (_sync)
        {
            _imageRefCount.TryGetValue(image, out var refCount);
            _imageRefCount[image] = refCount + 1;
        }
    }

    public nint GetOrLoadImage(Image<Rgba32> image)
    {
        lock (_sync)
        {
            if (_inputPointers.TryGetValue(image, out var texturePtr) && _ptrTextures.ContainsKey(texturePtr))
            {
                TouchTexture(texturePtr);
                return texturePtr;
            }

            if (_inputPointers.TryGetValue(image, out var stalePtr))
            {
                _inputPointers.Remove(image);
                _inputPointersReverse.Remove(stalePtr);
                _ptrUsedThisFrame.Remove(stalePtr);
            }

            var ptr = Load2DTexture(image);

            _inputPointers[image] = ptr;
            _inputPointersReverse[ptr] = image;
            TouchTexture(ptr);

            return ptr;
        }
    }

    public void UnregisterImage(Image<Rgba32> image)
    {
        lock (_sync)
        {
            if (!_imageRefCount.TryGetValue(image, out var refCount))
                return;

            if (refCount > 1)
            {
                _imageRefCount[image] = refCount - 1;
                return;
            }

            _imageRefCount.Remove(image);

            if (_inputPointers.TryGetValue(image, out var ptr))
                _unloadQueue.Add(ptr);
        }
    }

    public void UpdateImage(nint ptr)
    {
        lock (_sync)
        {
            if (!_ptrTextures.ContainsKey(ptr) || !_inputPointersReverse.TryGetValue(ptr, out Image<Rgba32>? texture))
                return;

            UpdateTexture(_ptrTextures[ptr], texture);
            TouchTexture(ptr);
        }
    }

    private void TouchTexture(nint ptr)
    {
        _ptrUsedThisFrame[ptr] = true;
    }

    private nint Load2DTexture(Image<Rgba32> image)
    {
        int texture = CreateTexture(image);
        nint imagePtr = texture;
        _ptrTextures[imagePtr] = texture;

        return imagePtr;
    }

    private static int CreateTexture(Image<Rgba32> image)
    {
        int texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        UpdateTexture(texture, image);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        return texture;
    }

    private static unsafe void UpdateTexture(int texture, Image<Rgba32> image)
    {
        var copiedImage = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(copiedImage);

        fixed (Rgba32* imgData = copiedImage)
        {
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (nint)imgData);
        }
    }

    internal void FreeTextures()
    {
        lock (_sync)
        {
            HashSet<nint>? toFree = null;

            foreach (var (ptr, usedThisFrame) in _ptrUsedThisFrame)
            {
                if (usedThisFrame)
                    continue;

                if (!_ptrTextures.ContainsKey(ptr))
                    continue;

                toFree ??= [];
                toFree.Add(ptr);
            }

            foreach (var ptr in _unloadQueue)
            {
                if (!_ptrTextures.ContainsKey(ptr))
                    continue;

                toFree ??= [];
                toFree.Add(ptr);
            }

            if (toFree is { Count: > 0 })
            {
                foreach (var ptr in toFree)
                {
                    if (!_ptrTextures.TryGetValue(ptr, out var texture))
                        continue;

                    GL.DeleteTexture(texture);
                    _ptrTextures.Remove(ptr);
                    _ptrUsedThisFrame.Remove(ptr);

                    if (!_inputPointersReverse.TryGetValue(ptr, out var image))
                        continue;

                    _inputPointersReverse.Remove(ptr);
                    _inputPointers.Remove(image);
                }
            }

            foreach (var ptr in _ptrUsedThisFrame.Keys)
                _ptrUsedThisFrame[ptr] = false;

            _unloadQueue.Clear();
        }
    }

    internal void Dispose()
    {
        lock (_sync)
        {
            foreach (var ptrTexture in _ptrTextures)
                GL.DeleteTexture(ptrTexture.Value);

            _ptrTextures.Clear();
            _imageRefCount.Clear();
            _ptrUsedThisFrame.Clear();
            _inputPointers.Clear();
            _inputPointersReverse.Clear();
            _unloadQueue.Clear();
        }
    }
}
*/