using System;
using Hexa.NET.ImGui;
using ImGui.Forms.Localization;
using ImGui.Forms.Resources;

namespace ImGui.Forms.Controls.Menu;

public class MenuBarCheckBox : MenuBarItem
{
    private bool _checked;

    #region Properties

    public LocalizedString Text { get; set; }

    public FontResource? Font { get; set; }

    public override int Height => GetHeight();

    public bool Checked
    {
        get => _checked;
        set
        {
            _checked = value;
            OnCheckedChanged();
        }
    }

    #endregion

    #region Events

    public event EventHandler CheckChanged;

    #endregion

    public MenuBarCheckBox(LocalizedString text = default)
    {
        Text = text;
    }

    protected override void UpdateInternal()
    {
        // Add menu check box
        if (Hexa.NET.ImGui.ImGui.MenuItem(Text, Checked, Enabled))
            // Invert checked value, if clicked
            Checked = !Checked;
    }

    protected override void UpdateEventsInternal()
    {
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

    private void OnCheckedChanged()
    {
        CheckChanged?.Invoke(this, EventArgs.Empty);
    }
}