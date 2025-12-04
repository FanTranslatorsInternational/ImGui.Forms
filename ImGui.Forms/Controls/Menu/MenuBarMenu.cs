using System.Collections.Generic;
using System.Numerics;
using ImGui.Forms.Localization;
using ImGui.Forms.Resources;
using ImGuiNET;

namespace ImGui.Forms.Controls.Menu;

public class MenuBarMenu : MenuBarItem
{
    #region Properties

    public LocalizedString Text { get; set; }

    public FontResource Font { get; set; }

    public Vector2 Padding { get; set; } = new(8, 8);

    public IList<MenuBarItem> Items { get; } = new List<MenuBarItem>();

    public override int Height => GetHeight();

    #endregion

    public MenuBarMenu(LocalizedString text = default)
    {
        Text = text;
    }

    protected override void UpdateInternal()
    {
        if (ImGuiNET.ImGui.BeginMenu(Text, Enabled))
        {
            foreach (var item in Items)
                item.Update();

            ImGuiNET.ImGui.EndMenu();
        }
        else
            UpdateEventsInternal();
    }

    protected override void UpdateEventsInternal()
    {
        if (!Enabled)
            return;

        foreach (var item in Items)
            item.UpdateEvents();
    }

    private int GetHeight()
    {
        ApplyStyles();

        var textSize = TextMeasurer.MeasureText(Text);
        var height = (int)(textSize.Y + ImGuiNET.ImGui.GetStyle().FramePadding.Y * 2);

        RemoveStyles();

        return height;
    }

    protected override void ApplyStyles()
    {
        ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4, 5));
        ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Padding);

        ImFontPtr? fontPtr = Font?.GetPointer();
        if (fontPtr != null)
            ImGuiNET.ImGui.PushFont(fontPtr.Value);
    }

    protected override void RemoveStyles()
    {
        if (Font?.GetPointer() != null)
            ImGuiNET.ImGui.PopFont();

        ImGuiNET.ImGui.PopStyleVar(2);
    }
}