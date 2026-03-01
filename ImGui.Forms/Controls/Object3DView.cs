using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Support;
using System;
using System.Numerics;

namespace ImGui.Forms.Controls;

public unsafe class Object3DView : Component
{
    private const float WheelScaleSpeed = 0.01f;
    private const float WheelRotationSpeed = 0.1f;
    private const float DragPositionSpeed = 0.01f;

    private readonly SdlGpuMeshRenderer3D _renderer;
    private ObjectState? _state;
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
        _state ??= new ObjectState();

        Hexa.NET.ImGui.ImGui.SetCursorScreenPos(contentRect.Position);
        Hexa.NET.ImGui.ImGui.InvisibleButton($"Object3DView_{Id}", contentRect.Size, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight);

        if (_mesh == null || _mesh.Faces.Count == 0)
            return;

        if (Hexa.NET.ImGui.ImGui.IsItemHovered())
        {
            var io = Hexa.NET.ImGui.ImGui.GetIO();
            float wheelDelta = io.MouseWheel;
            if (wheelDelta != 0f)
            {
                if (io.KeyCtrl)
                    _state.YRotation += wheelDelta * WheelRotationSpeed;
                else
                    _state.Scale = Math.Clamp(_state.Scale + wheelDelta * WheelScaleSpeed, 0.1f, 10f);
            }
        }

        if ((Hexa.NET.ImGui.ImGui.IsItemHovered() || Hexa.NET.ImGui.ImGui.IsItemActive()) && Hexa.NET.ImGui.ImGui.IsMouseDown(ImGuiMouseButton.Right))
        {
            Vector2 mouseDelta = Hexa.NET.ImGui.ImGui.GetIO().MouseDelta;
            if (mouseDelta != Vector2.Zero)
                _state.PositionOffset += new Vector3(mouseDelta.X * DragPositionSpeed, -mouseDelta.Y * DragPositionSpeed, 0f);
        }

        var transformation = Matrix4x4.CreateScale(_state.Scale) * Matrix4x4.CreateRotationY(_state.YRotation) * Matrix4x4.CreateTranslation(_state.PositionOffset);

        Application.Instance.EnqueueGpuPrepareAction((gpuDevice, commandBuffer) =>
        {
            _renderer.Prepare(gpuDevice, commandBuffer);
        });

        Application.Instance.EnqueueGpuRenderAction((gpuDevice, commandBuffer, renderPass) =>
        {
            _renderer.Render(gpuDevice, commandBuffer, renderPass, contentRect, transformation);
        });
    }

    public override void Destroy()
    {
        _renderer.Dispose();
    }
}

internal class ObjectState
{
    public Vector3 PositionOffset { get; set; }
    public float Scale { get; set; }
    public float YRotation { get; set; }
}
