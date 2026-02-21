using System;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Support;

namespace ImGui.Forms.Controls.Text;

public class MultiLineTextBox : Component
{
    private string _text = string.Empty;

    #region Properties

    /// <summary>
    /// The size of this component.
    /// </summary>
    public Size Size { get; set; } = Size.Parent;

    /// <summary>
    /// The text that was set or changed in this component.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            OnTextChanged();
        }
    }

    /// <summary>
    /// Marks the input as read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Get or set the max count of characters allowed in the text box.
    /// </summary>
    public uint MaxCharacters { get; set; } = 2048;

    #endregion

    #region Events

    public event EventHandler TextChanged;

    #endregion

    public override Size GetSize() => Size;

    protected override void UpdateInternal(Rectangle contentRect)
    {
        var enabled = Enabled;
        var isReadonly = IsReadOnly;

        var flags = ImGuiInputTextFlags.None;
        if (isReadonly || !enabled) flags |= ImGuiInputTextFlags.ReadOnly;

        if (isReadonly || !enabled)
        {
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.FrameBg, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
        }

        if (Hexa.NET.ImGui.ImGui.InputTextMultiline($"##{Id}", ref _text, MaxCharacters, contentRect.Size, flags))
            OnTextChanged();

        if (isReadonly || !enabled)
            Hexa.NET.ImGui.ImGui.PopStyleColor(3);
    }

    private void OnTextChanged()
    {
        TextChanged?.Invoke(this, EventArgs.Empty);
    }
}