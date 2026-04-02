namespace ImGui.Forms.Models;

public readonly struct MeshFace3D(int aIndex, int bIndex, int cIndex)
{
    public int AIndex { get; } = aIndex;
    public int BIndex { get; } = bIndex;
    public int CIndex { get; } = cIndex;
}
