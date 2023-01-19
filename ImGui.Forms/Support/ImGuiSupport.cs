using System.Numerics;
using ImGuiNET;

namespace ImGui.Forms.Support
{
    public static class ImGuiSupport
    {
        public static void Dummy(int id, Vector2 pos, Vector2 size)
        {
            ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, 0);
            ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
            ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);

            ImGuiNET.ImGui.SetCursorPos(pos);
            ImGuiNET.ImGui.Button($"##{id}", size);

            ImGuiNET.ImGui.PopStyleColor(3);
        }
    }
}
