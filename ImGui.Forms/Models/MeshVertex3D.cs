using System.Numerics;

namespace ImGui.Forms.Models;

public readonly struct MeshVertex3D
{
    public Vector3 Position { get; }
    public Vector4 Color { get; }
    public Vector2 UvCoordinate { get; }

    public MeshVertex3D(Vector3 position, Vector4 color, Vector2 uvCoordinate = default)
    {
        Position = position;
        Color = color;
        UvCoordinate = uvCoordinate;
    }
}
