using System;
using System.Numerics;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;
using ImGui.Forms.Support;

namespace ImGui.Forms.Controls;

public class ImageButton : Component
{
    private ThemedImageResource? _baseImg;

    #region Properties

    public LocalizedString? Tooltip { get; set; }

    public KeyCommand KeyAction { get; set; }

    public ThemedImageResource? Image
    {
        get => _baseImg;
        set
        {
            _baseImg?.Destroy();
            _baseImg = value;
        }
    }

    public Vector2 ImageSize { get; set; } = Vector2.Zero;

    public Vector2 Padding { get; set; } = new(2, 2);

    #endregion

    #region Events

    public event EventHandler Clicked;

    #endregion

    public ImageButton(ThemedImageResource? image = null)
    {
        Image = image;
    }

    public override Size GetSize()
    {
        var size = GetImageSize();
        return new Size((int)size.X + (int)Padding.X * 2, (int)size.Y + (int)Padding.Y * 2);
    }

    protected override void UpdateInternal(Rectangle contentRect)
    {
        var enabled = Enabled;
        ApplyStyles(enabled);

        if (Image != null && Image.IsValid())
        {
            if ((Hexa.NET.ImGui.ImGui.ImageButton($"##{Id}", Image.GetTextureRef(), GetImageSize()) || KeyAction.IsPressed()) && Enabled)
                OnClicked();
        }
        else
        {
            if ((Hexa.NET.ImGui.ImGui.Button(string.Empty, GetImageSize() + Padding * 2) || KeyAction.IsPressed()) && Enabled)
                OnClicked();
        }

        if (Enabled && Tooltip is { IsEmpty: false } && Hexa.NET.ImGui.ImGui.IsItemHovered())
        {
            Hexa.NET.ImGui.ImGui.BeginTooltip();
            Hexa.NET.ImGui.ImGui.Text(Tooltip);
            Hexa.NET.ImGui.ImGui.EndTooltip();
        }

        RemoveStyles(enabled);
    }

    private void ApplyStyles(bool enabled)
    {
        if (!enabled)
        {
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.Button, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.ButtonActive, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
        }

        Hexa.NET.ImGui.ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Padding);
    }

    private void RemoveStyles(bool enabled)
    {
        Hexa.NET.ImGui.ImGui.PopStyleVar();

        if (!enabled)
            Hexa.NET.ImGui.ImGui.PopStyleColor(3);
    }

    private Vector2 GetImageSize()
    {
        return ImageSize != Vector2.Zero ? ImageSize : Image?.Size ?? Vector2.Zero;
    }

    private void OnClicked()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}