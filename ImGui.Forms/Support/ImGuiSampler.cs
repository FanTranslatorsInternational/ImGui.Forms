using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Hexa.NET.SDL3;

namespace ImGui.Forms.Support;

internal static class ImGuiSampler
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ImGuiImplSDLGPU3RenderState
    {
        public nint Device;
        public nint SamplerDefault;
        public nint SamplerCurrent;
    }

    private static SDLGPUSamplerPtr _nearestSampler;

    private static readonly unsafe ImDrawCallback SetNearestSamplerCallback = SetNearestSampler;

    public static void Initialize(SDLGPUDevicePtr gpuDevice)
    {
        _nearestSampler = SDL.CreateGPUSampler(gpuDevice, new SDLGPUSamplerCreateInfo
        {
            MinFilter = SDLGPUFilter.Nearest,
            MagFilter = SDLGPUFilter.Nearest,
            MipmapMode = SDLGPUSamplerMipmapMode.Nearest
        });
    }

    public static unsafe void SetNearest()
    {
        ImDrawListPtr drawList = Hexa.NET.ImGui.ImGui.GetWindowDrawList();
        drawList.AddCallback(SetNearestSamplerCallback, null);
    }

    public static void Release(SDLGPUDevicePtr gpuDevice)
    {
        SDL.ReleaseGPUSampler(gpuDevice, _nearestSampler);
    }

    private static unsafe void SetNearestSampler(ImDrawList* _, ImDrawCmd* __)
    {
        var platformIo = Hexa.NET.ImGui.ImGui.GetPlatformIO();
        var renderState = (ImGuiImplSDLGPU3RenderState*)platformIo.RendererRenderState;
        if (renderState == null)
            return;

        renderState->SamplerCurrent = (nint)_nearestSampler.Handle;
    }
}
