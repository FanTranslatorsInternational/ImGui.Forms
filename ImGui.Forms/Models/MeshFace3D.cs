namespace ImGui.Forms.Models;

public readonly struct MeshFace3D(MeshVertex3D a, MeshVertex3D b, MeshVertex3D c)
{
    public MeshVertex3D A { get; } = a;
    public MeshVertex3D B { get; } = b;
    public MeshVertex3D C { get; } = c;
}
