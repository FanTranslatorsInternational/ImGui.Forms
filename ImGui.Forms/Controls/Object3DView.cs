using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Support;

namespace ImGui.Forms.Controls;

public unsafe class Object3DView : Component
{
    private readonly SdlGpuMeshRenderer3D _renderer;
    private Mesh3D? _mesh;

    public Size Size { get; set; } = Size.Parent;

    public Mesh3D? Mesh
    {
        get => _mesh;
        set
        {
            _mesh = value;
            _renderer.SetMesh(_mesh);
        }
    }

    public Object3DView(Mesh3D? mesh = null)
    {
        _renderer = new SdlGpuMeshRenderer3D(mesh);
        _mesh = mesh;
    }

    public override Size GetSize()
    {
        return Size;
    }

    protected override void UpdateInternal(Rectangle contentRect)
    {
        Hexa.NET.ImGui.ImGui.SetCursorScreenPos(contentRect.Position);
        Hexa.NET.ImGui.ImGui.InvisibleButton($"Object3DView_{Id}", contentRect.Size, ImGuiButtonFlags.MouseButtonLeft);

        if (_mesh == null || _mesh.Faces.Count == 0)
            return;

        Application.Instance.EnqueueGpuPrepareAction((gpuDevice, commandBuffer) =>
        {
            _renderer.Prepare(gpuDevice, commandBuffer);
        });

        Application.Instance.EnqueueGpuRenderAction((gpuDevice, commandBuffer, renderPass) =>
        {
            _renderer.Render(gpuDevice, commandBuffer, renderPass, contentRect);
        });
    }

    public override void Destroy()
    {
        _renderer.Dispose();
    }
}
