using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls;

public class ArrowButton : Component
{
    private const int ButtonSizeX_ = 11;
    private const int ButtonSizeY_ = 13;

    #region Properties

    public KeyCommand KeyAction { get; set; }

    public ImGuiDir Direction { get; set; }

    public Vector2 Padding { get; set; } = new(4, 3);

    #endregion

    #region Events

    public event EventHandler Clicked;

    #endregion

    public ArrowButton(ImGuiDir direction = ImGuiDir.None)
    {
        Direction = direction;
    }

    public override Size GetSize()
    {
        return new Size((int)Math.Ceiling(ButtonSizeX_ + Padding.X * 2), (int)Math.Ceiling(ButtonSizeY_ + Padding.Y * 2));
    }

    protected override void UpdateInternal(Rectangle contentRect)
    {
        var enabled = Enabled;

        ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Padding);

        if (!enabled)
        {
            ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
        }

        if ((ImGuiNET.ImGui.ArrowButton($"##{Id}", Direction) || KeyAction.IsPressed()) && Enabled)
            OnClicked();

        if (!enabled)
            ImGuiNET.ImGui.PopStyleColor(3);

        ImGuiNET.ImGui.PopStyleVar();
    }

    private void OnClicked()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}