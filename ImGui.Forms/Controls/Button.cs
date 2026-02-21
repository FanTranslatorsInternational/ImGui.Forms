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

public class Button : Component
{
    #region Properties

    public LocalizedString Text { get; set; }
    public LocalizedString Tooltip { get; set; }
    public FontResource? Font { get; set; }

    public KeyCommand KeyAction { get; set; }

    public Vector2 Padding { get; set; } = new(2, 2);

    public SizeValue Width { get; set; } = SizeValue.Content;

    #endregion

    #region Events

    public event EventHandler Clicked;

    #endregion

    public Button(LocalizedString text = default)
    {
        Text = text;
    }

    public override Size GetSize()
    {
        ApplyStyles(Enabled, Font);

        var textSize = TextMeasurer.MeasureText(EscapeText());
        SizeValue width = Width.IsContentAligned ? (int)Math.Ceiling(textSize.X) + (int)Padding.X * 2 : Width;
        var height = (int)Math.Ceiling(textSize.Y) + (int)Padding.Y * 2;

        RemoveStyles(Enabled, Font);

        return new Size(width, height);
    }

    protected override void UpdateInternal(Rectangle contentRect)
    {
        var enabled = Enabled;
        var font = Font;

        ApplyStyles(enabled, font);

        if ((Hexa.NET.ImGui.ImGui.Button(EscapeText(), contentRect.Size) || KeyAction.IsPressed()) && Enabled)
            OnClicked();

        if (Tooltip is { IsEmpty: false } && Hexa.NET.ImGui.ImGui.IsItemHovered())
        {
            Hexa.NET.ImGui.ImGui.BeginTooltip();
            Hexa.NET.ImGui.ImGui.Text(Tooltip);
            Hexa.NET.ImGui.ImGui.EndTooltip();
        }

        RemoveStyles(enabled, font);
    }

    private void ApplyStyles(bool enabled, FontResource? font)
    {
        if (!enabled)
        {
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.Button, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.ButtonActive, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
        }

        ImFontPtr? fontPtr = font?.GetPointer();
        if (fontPtr != null)
            Hexa.NET.ImGui.ImGui.PushFont(fontPtr.Value, font!.Data.Size);

        Hexa.NET.ImGui.ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Padding);
    }

    private void RemoveStyles(bool enabled, FontResource? font)
    {
        Hexa.NET.ImGui.ImGui.PopStyleVar();
            
        if (font?.GetPointer() != null)
            Hexa.NET.ImGui.ImGui.PopFont();

        if (!enabled)
            Hexa.NET.ImGui.ImGui.PopStyleColor(3);
    }

    protected void OnClicked()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    protected string EscapeText()
    {
        return Text.ToString().Replace("\\n", Environment.NewLine);
    }
}