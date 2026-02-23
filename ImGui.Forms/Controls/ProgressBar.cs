using System;
using System.Numerics;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Localization;
using ImGui.Forms.Resources;
using SixLabors.ImageSharp;
using Rectangle = ImGui.Forms.Support.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls;

public class ProgressBar : Component
{
    #region Properties

    public LocalizedString Text { get; set; }

    public FontResource? Font { get; set; }

    public Size Size { get; set; } = Size.Parent;

    public ThemedColor ProgressColor { get; set; } = Color.FromRgba(0x27, 0xBB, 0x65, 0xFF);

    public int Minimum { get; set; }

    public int Maximum { get; set; } = 100;

    public int Value { get; set; }

    #endregion

    public override Size GetSize() => Size;

    protected override unsafe void UpdateInternal(Rectangle contentRect)
    {
        // Draw progress bar
        var trueMinimum = Math.Min(Minimum, Maximum);
        var trueMaximum = Math.Max(Minimum, Maximum);

        var range = trueMaximum - trueMinimum;
        var value = Math.Clamp(Value, trueMinimum, trueMaximum) - trueMinimum;

        if (range <= 0)
            range = 1;

        var barWidth = (float)Math.Ceiling(contentRect.Width / range * value);
        Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRectFilled(new Vector2(contentRect.X, contentRect.Y), new Vector2(contentRect.X + barWidth, contentRect.Y + contentRect.Height), ProgressColor.ToUInt32());

        // Draw border
        Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRect(new Vector2(contentRect.X, contentRect.Y), new Vector2(contentRect.X + contentRect.Width, contentRect.Y + contentRect.Height), Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.Border));

        // Draw text
        var textSize = TextMeasurer.MeasureText(Text);
        var textPos = new Vector2(contentRect.X + (contentRect.Width - textSize.X) / 2, contentRect.Y + (contentRect.Height - textSize.Y) / 2);

        ImFontPtr? fontPtr = Font?.GetPointer();
        if (fontPtr != null)
            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddText(fontPtr.Value, Font!.Data.Size, textPos, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.Text), Text);
        else
            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddText(textPos, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.Text), Text);
    }

    protected override void ApplyStyles()
    {
        ImFontPtr? fontPtr = Font?.GetPointer();
        if (fontPtr != null)
            Hexa.NET.ImGui.ImGui.PushFont(fontPtr.Value, Font!.Data.Size);
    }

    protected override void RemoveStyles()
    {
        if (Font?.GetPointer() != null)
            Hexa.NET.ImGui.ImGui.PopFont();
    }
}