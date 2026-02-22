using Hexa.NET.SDL3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImGui.Forms.Factories;

class ImageFactory(SDLGPUDevicePtr gpuDevice)
{
    private readonly Dictionary<Image<Rgba32>, nint> _inputPointers = [];
    private readonly Dictionary<nint, Image<Rgba32>> _inputPointersReverse = [];
    private readonly Dictionary<nint, SDLGPUTexturePtr> _ptrTextures = [];
    private readonly Dictionary<nint, int> _ptrTexturesRefCount = [];
    private readonly List<nint> _unloadQueue = [];

    public nint LoadImage(Image<Rgba32> img)
    {
        nint ptr;

        if (_inputPointers.TryGetValue(img, out IntPtr texturePtr))
        {
            ptr = texturePtr;
            UpdateImage(ptr);

            _ptrTexturesRefCount[ptr]++;

            return ptr;
        }

        ptr = Load2DTexture(img);

        _inputPointers[img] = ptr;
        _inputPointersReverse[ptr] = img;
        _ptrTexturesRefCount[ptr] = 1;

        return ptr;
    }

    public void UpdateImage(nint ptr)
    {
        if (!_ptrTextures.ContainsKey(ptr) || !_inputPointersReverse.TryGetValue(ptr, out Image<Rgba32>? texture))
            return;

        TransferToGpuTexture(_ptrTextures[ptr], texture);
    }

    public void UnloadImage(nint ptr)
    {
        if (!_ptrTextures.ContainsKey(ptr))
            return;

        _ptrTexturesRefCount[ptr]--;
        _unloadQueue.Add(ptr);
    }

    public void BindTextures(SDLGPURenderPassPtr renderPass)
    {
        if (_ptrTextures.Values.Count <= 0)
            return;

        SDLGPUSamplerCreateInfo sampInfo = new SDLGPUSamplerCreateInfo
        {
            MinFilter = SDLGPUFilter.Nearest,
            MagFilter = SDLGPUFilter.Nearest,
            MipmapMode = SDLGPUSamplerMipmapMode.Nearest
        };

        SDLGPUSamplerPtr nearestSampler = SDL.CreateGPUSampler(gpuDevice, sampInfo);

        var binds = new SDLGPUTextureSamplerBinding[_ptrTextures.Values.Count];
        for (var i = 0; i < binds.Length; i++)
        {
            SDLGPUTexturePtr texture = _ptrTextures.Values.ElementAt(i);

            SDLGPUTextureSamplerBinding bind = new SDLGPUTextureSamplerBinding
            {
                Texture = texture,
                Sampler = nearestSampler
            };
            binds[i] = bind;
        }

        SDL.BindGPUFragmentSamplers(renderPass, 0u, binds[0], (uint)binds.Length);
        SDL.BindGPUVertexSamplers(renderPass, 0u, binds[0], (uint)binds.Length);
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
        foreach (var toFree in _unloadQueue)
        {
            // Textures that were marked to unload, but somehow retained/regained references afterwards should not be unloaded
            if (!_ptrTexturesRefCount.ContainsKey(toFree) || _ptrTexturesRefCount[toFree] > 0)
                continue;

            SDL.ReleaseGPUTexture(gpuDevice, _ptrTextures[toFree]);

            _ptrTextures.Remove(toFree);
            _ptrTexturesRefCount.Remove(toFree);

            var obj = _inputPointersReverse[toFree];
            _inputPointersReverse.Remove(toFree);
            _inputPointers.Remove(obj);
        }

        _unloadQueue.Clear();
    }
}