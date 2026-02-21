using System;
using System.Numerics;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Support;
using Size = ImGui.Forms.Models.Size;

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

        Hexa.NET.ImGui.ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Padding);

        if (!enabled)
        {
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.Button, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.ButtonActive, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
        }

        if ((Hexa.NET.ImGui.ImGui.ArrowButton($"##{Id}", Direction) || KeyAction.IsPressed()) && Enabled)
            OnClicked();

        if (!enabled)
            Hexa.NET.ImGui.ImGui.PopStyleColor(3);

        Hexa.NET.ImGui.ImGui.PopStyleVar();
    }

    private void OnClicked()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}