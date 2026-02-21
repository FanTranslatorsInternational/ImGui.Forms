using System;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = ImGui.Forms.Support.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls;

public class ColorPicker : Component
{
    public bool UseAlpha { get; set; } = true;

    public Rgba32 PickedColor { get; set; }

    public event EventHandler ColorChanged;

    public override Size GetSize() => Size.Parent;

    protected override void UpdateInternal(Rectangle contentRect)
    {
        var flags = ImGuiColorEditFlags.NoSidePreview;

        if (UseAlpha)
        {
            flags |= ImGuiColorEditFlags.AlphaBar;

            var pickedColor = ((Color)PickedColor).ToVector4();
            if (Hexa.NET.ImGui.ImGui.ColorPicker4($"##{Id}", ref pickedColor, flags))
            {
                PickedColor = pickedColor.ToColor();
                OnColorChanged();
            }
        }
        else
        {
            var pickedColor = ((Color)PickedColor).ToVector3();
            if (Hexa.NET.ImGui.ImGui.ColorPicker3($"##{Id}", ref pickedColor, flags))
            {
                PickedColor = pickedColor.ToColor();
                OnColorChanged();
            }
        }
    }

    private void OnColorChanged()
    {
        ColorChanged?.Invoke(this, EventArgs.Empty);
    }
}