using System;
using System.Numerics;

namespace ImGui.Forms.Models;

public sealed class SceneConfiguration
{
    public bool ShowGrid { get; set; }
    public bool ShowVertices { get; set; }
    public bool ShowWireFrame { get; set; } = true;

    public float VertexDotSize { get; set; } = 4f;
    public Vector4 VertexDotColor { get; set; } = Vector4.One;

    public float WireThickness { get; set; } = 1.25f;
    public Vector4 WireColor { get; set; } = Vector4.One;

    public Vector3 LightDirection { get; set; } = new(1f, 0f, -1f);
    public Vector3 LightColor { get; set; } = Vector3.One;
    public float LightIntensity { get; set; } = 1f;

    public float ZoomSpeed { get; set; } = 0.1f;
    public float MinDistance { get; set; } = 0.25f;
    public float MaxDistance { get; set; } = 100f;
    public float RotationSpeed { get; set; } = 0.005f;
    public float PanSpeed { get; set; } = 2f;
    public float PitchLimit { get; set; } = MathF.PI * 0.49f;
}
