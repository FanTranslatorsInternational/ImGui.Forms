using System.Collections.Generic;

namespace ImGui.Forms.Models;

public sealed class Mesh3D(IEnumerable<MeshFace3D> faces)
{
    public IReadOnlyList<MeshFace3D> Faces { get; } = [..faces];
}
