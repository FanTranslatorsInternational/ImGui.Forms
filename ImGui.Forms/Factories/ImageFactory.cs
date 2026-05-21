using Hexa.NET.SDL3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;

namespace ImGui.Forms.Factories;

internal class ImageFactory(SDLGPUDevicePtr gpuDevice)
{
    private readonly Dictionary<Image<Rgba32>, nint> _inputPointers = [];
    private readonly Dictionary<nint, Image<Rgba32>> _inputPointersReverse = [];
    private readonly Dictionary<nint, SDLGPUTexturePtr> _ptrTextures = [];
    private readonly Dictionary<Image<Rgba32>, int> _imageRefCount = [];
    private readonly Dictionary<nint, bool> _ptrUsedThisFrame = [];
    private readonly List<nint> _unloadQueue = [];

    public void RegisterImage(Image<Rgba32> image)
    {
        _imageRefCount.TryGetValue(image, out var refCount);
        _imageRefCount[image] = refCount + 1;
    }

    public nint GetOrLoadImage(Image<Rgba32> image)
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

    public void UnregisterImage(Image<Rgba32> image)
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

    public void UpdateImage(nint ptr)
    {
        if (!_ptrTextures.ContainsKey(ptr) || !_inputPointersReverse.TryGetValue(ptr, out Image<Rgba32>? texture))
            return;

        TransferToGpuTexture(_ptrTextures[ptr], texture);
        TouchTexture(ptr);
    }

    private void TouchTexture(nint ptr)
    {
        _ptrUsedThisFrame[ptr] = true;
    }

    private unsafe nint Load2DTexture(Image<Rgba32> image)
    {
        SDLGPUTexturePtr gpuTexture = CreateGpuTexture(image);

        // Add image pointer to cache
        var imgPtr = (nint)gpuTexture.Handle;
        _ptrTextures[imgPtr] = gpuTexture;

        return imgPtr;
    }

    private SDLGPUTexturePtr CreateGpuTexture(Image<Rgba32> image)
    {
        SDLGPUTexturePtr gpuTexture = SDL.CreateGPUTexture(gpuDevice, new SDLGPUTextureCreateInfo
        {
            Width = (uint)image.Width,
            Height = (uint)image.Height,
            Format = SDLGPUTextureFormat.R8G8B8A8Unorm,
            Type = SDLGPUTextureType.Texturetype2D,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDLGPUSampleCount.Samplecount1,
            Usage = (int)SDLGPUTextureUsageFlags.Sampler
        });

        TransferToGpuTexture(gpuTexture, image);

        return gpuTexture;
    }

    private unsafe void TransferToGpuTexture(SDLGPUTexturePtr gpuTexture, Image<Rgba32> image)
    {
        // Transfer image into temporary buffer
        int size = image.Width * image.Height * 4;

        SDLGPUTransferBufferPtr transferBuffer = SDL.CreateGPUTransferBuffer(gpuDevice, new SDLGPUTransferBufferCreateInfo
        {
            Size = (uint)size,
            Usage = SDLGPUTransferBufferUsage.Upload
        });

        void* texturePtr = SDL.MapGPUTransferBuffer(gpuDevice, transferBuffer, true);

        var copiedImage = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(copiedImage);

        fixed (Rgba32* imgData = copiedImage)
            Buffer.MemoryCopy(imgData, texturePtr, size, size);

        SDL.UnmapGPUTransferBuffer(gpuDevice, transferBuffer);

        // Upload texture
        var transferInfo = new SDLGPUTextureTransferInfo
        {
            Offset = 0,
            TransferBuffer = transferBuffer
        };

        var textureRegion = new SDLGPUTextureRegion
        {
            Texture = gpuTexture,
            X = 0,
            Y = 0,
            W = (uint)image.Width,
            H = (uint)image.Height,
            D = 1
        };

        SDLGPUCommandBufferPtr cmd = SDL.AcquireGPUCommandBuffer(gpuDevice);
        SDLGPUCopyPassPtr copyPass = SDL.BeginGPUCopyPass(cmd);
        SDL.UploadToGPUTexture(copyPass, transferInfo, textureRegion, false);
        SDL.EndGPUCopyPass(copyPass);
        SDL.SubmitGPUCommandBuffer(cmd);

        SDL.ReleaseGPUTransferBuffer(gpuDevice, transferBuffer);
    }

    internal void FreeTextures()
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
            SDL.WaitForGPUIdle(gpuDevice);

            foreach (var ptr in toFree)
            {
                if (!_ptrTextures.TryGetValue(ptr, out var texture))
                    continue;

                SDL.ReleaseGPUTexture(gpuDevice, texture);
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

    internal void Dispose()
    {
        foreach (var ptrTexture in _ptrTextures)
            SDL.ReleaseGPUTexture(gpuDevice, ptrTexture.Value);

        _ptrTextures.Clear();
        _imageRefCount.Clear();
        _ptrUsedThisFrame.Clear();
        _inputPointers.Clear();
        _inputPointersReverse.Clear();
        _unloadQueue.Clear();
    }
}