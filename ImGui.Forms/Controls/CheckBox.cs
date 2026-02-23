using System;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGui.Forms.Support;

namespace ImGui.Forms.Controls;

public class CheckBox : Component
{
    private bool _checked;

    #region Properties

    public LocalizedString Text { get; set; }
    public LocalizedString Tooltip { get; set; }
    public FontResource? Font { get; set; }

    public bool Checked
    {
        get => _checked;
        set
        {
            _checked = value;
            OnCheckChanged();
        }
    }

    #endregion

    #region Events

    public event EventHandler CheckChanged;

    #endregion

    public CheckBox(LocalizedString text = default)
    {
        Text = text;
    }

    public override Size GetSize()
    {
        ApplyStyles(Enabled, Font);

        var size = TextMeasurer.MeasureText(Text);
        var width = (int)(Math.Ceiling(size.X) + 21 + Hexa.NET.ImGui.ImGui.GetStyle().ItemInnerSpacing.X);
        var height = (int)Math.Max(Math.Ceiling(size.Y), 21);

        RemoveStyles(Enabled, Font);

        return new Size(width, height);
    }

    protected override void UpdateInternal(Rectangle contentRect)
    {
        Hexa.NET.ImGui.ImGui.SetNextItemWidth(contentRect.Width);

        var check = Checked;
        var enabled = Enabled;
        var font = Font;

        ApplyStyles(enabled, font);

        if (IsHovering(contentRect) && !string.IsNullOrEmpty(Tooltip))
            Hexa.NET.ImGui.ImGui.SetTooltip(Tooltip);

        if (Hexa.NET.ImGui.ImGui.Checkbox(Text, ref check) && Enabled)
            Checked = check;

        RemoveStyles(enabled, font);
    }

    private void ApplyStyles(bool enabled, FontResource? font)
    {
        if (!enabled)
        {
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.CheckMark, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.Text));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.FrameBg, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
        }

        ImFontPtr? fontPtr = font?.GetPointer();
        if (fontPtr != null)
            Hexa.NET.ImGui.ImGui.PushFont(fontPtr.Value, font!.Data.Size);
    }

    private void RemoveStyles(bool enabled, FontResource? font)
    {
        if (font?.GetPointer() != null)
            Hexa.NET.ImGui.ImGui.PopFont();

        if (!enabled)
            Hexa.NET.ImGui.ImGui.PopStyleColor(4);
    }

    private bool IsHovering(Rectangle contentRect)
    {
        return Hexa.NET.ImGui.ImGui.IsMouseHoveringRect(contentRect.Position, contentRect.Position + contentRect.Size);
    }

    private void OnCheckChanged()
    {
        CheckChanged?.Invoke(this, EventArgs.Empty);
    }
}