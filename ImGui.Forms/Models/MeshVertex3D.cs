using System.Numerics;

namespace ImGui.Forms.Models;

public readonly struct MeshVertex3D(Vector3 position, Vector4 color)
{
    public Vector3 Position { get; } = position;
    public Vector4 Color { get; } = color;
}
