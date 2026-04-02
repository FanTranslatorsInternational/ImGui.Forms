using System.Collections.Generic;

namespace ImGui.Forms.Models;

public sealed class Mesh3D(IList<MeshVertex3D> vertices, IList<MeshFace3D> faces)
{
    public IList<MeshVertex3D> Vertices { get; } = vertices;
    public IList<MeshFace3D> Faces { get; } = faces;
}
