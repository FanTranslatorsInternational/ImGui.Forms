using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGui.Forms.Support;
using System;
using System.Collections.Generic;
using System.Numerics;
using Image = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using Rectangle = ImGui.Forms.Support.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls;

public unsafe class Object3DView : Component
{
    private const float ZoomSpeed = 0.1f;
    private const float MinDistance = 0.25f;
    private const float MaxDistance = 100f;
    private const float RotationSpeed = 0.005f;
    private const float PanSpeed = 2f;
    private const float PitchLimit = MathF.PI * 0.49f;

    private readonly SdlGpuMeshRenderer3D _renderer;
    private readonly OrbitCameraState _camera;
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

            ResetCamera();
        }
    }

    public Image? Texture
    {
        set => _renderer.SetTexture(value);
    }

    public bool ShowWireFrame
    {
        get => _renderer.GetWireframeEnabled();
        set => _renderer.SetWireframeEnabled(value);
    }

    public bool ShowGrid
    {
        get => _renderer.GetGridEnabled();
        set => _renderer.SetGridEnabled(value);
    }

    public IEnumerable<Rectangle>? ScissorExclusions
    {
        set => _renderer.SetAdditionalScissorExclusions(value);
    }

    public Object3DView(Mesh3D? mesh = null)
    {
        _renderer = new SdlGpuMeshRenderer3D(mesh);
        _camera = new OrbitCameraState();
        _mesh = mesh;
    }

    public override Size GetSize()
    {
        return Size;
    }

    protected override void UpdateInternal(Rectangle contentRect)
    {
        Hexa.NET.ImGui.ImGui.Dummy(contentRect.Size);

        if (_mesh == null || _mesh.Faces.Count == 0)
            return;

        Process3DObject(contentRect);
        ProcessOverlay(contentRect);
    }

    public override void Destroy()
    {
        _renderer.Dispose();
    }

    private void Process3DObject(Rectangle contentRect)
    {
        int viewportWidth = Math.Max(1, (int)contentRect.Width);
        int viewportHeight = Math.Max(1, (int)contentRect.Height);

        _state ??= new ObjectState
        {
            Projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, viewportWidth / (float)viewportHeight, 0.1f, 100f)
        };
        _state.Projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, viewportWidth / (float)viewportHeight, 0.1f, 100f);

        bool isFocused = Hexa.NET.ImGui.ImGui.IsItemHovered() || Hexa.NET.ImGui.ImGui.IsItemActive();
        if (isFocused)
        {
            var io = Hexa.NET.ImGui.ImGui.GetIO();
            var minViewportDimension = MathF.Max(1f, MathF.Min(viewportWidth, viewportHeight));
            float panScale = _camera.Distance * PanSpeed / minViewportDimension;

            // Mouse wheel zooms the orbit camera.
            if (io.MouseWheel != 0f)
            {
                float zoomFactor = 1f - (io.MouseWheel * ZoomSpeed);
                if (zoomFactor > 0f)
                    _camera.Distance = Math.Clamp(_camera.Distance * zoomFactor, MinDistance, MaxDistance);
            }

            var (_, cameraRight, cameraUp) = GetCameraBasis(_camera.Yaw, _camera.Pitch);

            // Left drag pans the camera target in the view plane.
            if (Hexa.NET.ImGui.ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                _camera.Target += (-io.MouseDelta.X * panScale * cameraRight) + (io.MouseDelta.Y * panScale * cameraUp);
            }

            // Right drag orbits camera around target.
            if (Hexa.NET.ImGui.ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                _camera.Yaw += io.MouseDelta.X * RotationSpeed;
                _camera.Pitch = Math.Clamp(_camera.Pitch + -io.MouseDelta.Y * RotationSpeed, -PitchLimit, PitchLimit);
            }
        }

        var (forward, _, up) = GetCameraBasis(_camera.Yaw, _camera.Pitch);
        Vector3 cameraPosition = _camera.Target - (forward * _camera.Distance);
        _state.View = Matrix4x4.CreateLookAt(cameraPosition, _camera.Target, up);
        _state.Transformation = Matrix4x4.Identity;

        Application.Instance.EnqueueGpuPrepareAction((gpuDevice, commandBuffer) =>
        {
            _renderer.Prepare(gpuDevice, commandBuffer);
        });

        Application.Instance.EnqueueGpuRenderAction((gpuDevice, commandBuffer, renderPass) =>
        {
            _renderer.Render(gpuDevice, commandBuffer, renderPass, contentRect, _state);
        });
    }

    private void ProcessOverlay(Rectangle contentRect)
    {
        Hexa.NET.ImGui.ImGui.SetCursorScreenPos(contentRect.Position);

        var showWireFrame = _renderer.GetWireframeEnabled();
        var showGrid = _renderer.GetGridEnabled();

        Hexa.NET.ImGui.ImGui.PushID($"{Id}-wire");
        Hexa.NET.ImGui.ImGui.Checkbox(LocalizationResources.ShowWireFrameText(), ref showWireFrame);
        Hexa.NET.ImGui.ImGui.SameLine();
        var wireFrameLength = Hexa.NET.ImGui.ImGui.GetCursorScreenPos().X;
        Hexa.NET.ImGui.ImGui.NewLine();
        Hexa.NET.ImGui.ImGui.PopID();

        Hexa.NET.ImGui.ImGui.PushID($"{Id}-grid");
        Hexa.NET.ImGui.ImGui.Checkbox(LocalizationResources.ShowGridText(), ref showGrid);
        Hexa.NET.ImGui.ImGui.SameLine();
        var gridLength = Hexa.NET.ImGui.ImGui.GetCursorScreenPos().X;
        Hexa.NET.ImGui.ImGui.NewLine();
        Hexa.NET.ImGui.ImGui.PopID();

        _renderer.SetWireframeEnabled(showWireFrame);
        _renderer.SetGridEnabled(showGrid);

        var overlayWidth = Math.Max(wireFrameLength, gridLength) - contentRect.Position.X;
        var overlayHeight = Hexa.NET.ImGui.ImGui.GetCursorScreenPos().Y - contentRect.Position.Y;
        _renderer.SetAdditionalScissorExclusions([new Rectangle(contentRect.Position, new Vector2(overlayWidth, overlayHeight))]);
    }

    private static (Vector3 Forward, Vector3 Right, Vector3 Up) GetCameraBasis(float yaw, float pitch)
    {
        Vector3 worldUp = Vector3.UnitY;
        Vector3 forward = Vector3.Normalize(new Vector3(
            MathF.Cos(pitch) * MathF.Sin(yaw),
            MathF.Sin(pitch),
            -MathF.Cos(pitch) * MathF.Cos(yaw)));
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, worldUp));
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, forward));
        return (forward, right, up);
    }

    private void ResetCamera()
    {
        _camera.Target = Vector3.Zero;
        _camera.Distance = 3f;
        _camera.Yaw = 0f;
        _camera.Pitch = 0f;
    }
}

internal class OrbitCameraState
{
    public Vector3 Target { get; set; } = Vector3.Zero;
    public float Distance { get; set; } = 3f;
    public float Yaw { get; set; }
    public float Pitch { get; set; }
}

internal class ObjectState
{
    public Matrix4x4 Projection { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 View { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 Transformation { get; set; } = Matrix4x4.Identity;
}
