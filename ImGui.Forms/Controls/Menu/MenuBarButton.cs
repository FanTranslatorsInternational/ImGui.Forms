using System;
using Hexa.NET.ImGui;
using ImGui.Forms.Localization;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;

namespace ImGui.Forms.Controls.Menu;

public class MenuBarButton : MenuBarItem
{
    #region Properties

    public LocalizedString Text { get; set; }

    public FontResource? Font { get; set; }

    public KeyCommand KeyAction { get; set; }

    public override int Height => GetHeight();

    #endregion

    #region Events

    public event EventHandler Clicked;

    #endregion

    public MenuBarButton(LocalizedString text = default)
    {
        Text = text;
    }

    protected override void UpdateInternal()
    {
        // Add menu button
        if ((Hexa.NET.ImGui.ImGui.MenuItem(Text, KeyAction.Name, false, Enabled) || KeyAction.IsPressed()) && Enabled)
            // Execute click event, if set
            Clicked?.Invoke(this, EventArgs.Empty);
    }

    protected override void UpdateEventsInternal()
    {
        // Add menu button
        if (KeyAction.IsPressed() && Enabled)
            // Execute click event, if set
            Clicked?.Invoke(this, EventArgs.Empty);
    }

    private int GetHeight()
    {
        ApplyStyles();

        var textSize = TextMeasurer.MeasureText(Text);
        var height = (int)(textSize.Y + Hexa.NET.ImGui.ImGui.GetStyle().FramePadding.Y * 2);

        RemoveStyles();

        return height;
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