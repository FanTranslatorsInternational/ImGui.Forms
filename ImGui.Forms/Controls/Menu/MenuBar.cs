using System.Collections.Generic;
using System.Linq;
using Hexa.NET.ImGui;
using ImGui.Forms.Resources;

namespace ImGui.Forms.Controls.Menu;

public abstract class MenuBar
{
    private readonly bool _isMain;

    public IList<MenuBarItem> Items { get; } = new List<MenuBarItem>();

    public FontResource? Font { get; set; }

    public int Height => GetHeight();

    protected MenuBar(bool isMain)
    {
        _isMain = isMain;
    }

    public void Update()
    {
        ImFontPtr? fontPtr = Font?.GetPointer();
        if (fontPtr != null)
            Hexa.NET.ImGui.ImGui.PushFont(fontPtr.Value, Font!.Data.Size);

        // Begin menu bar
        bool isMenuOpen = _isMain ?
            Hexa.NET.ImGui.ImGui.BeginMainMenuBar() :
            Hexa.NET.ImGui.ImGui.BeginMenuBar();
        if (isMenuOpen)
        {
            // Add content to menu bar
            foreach (var child in Items)
                child.Update();

            if (_isMain) Hexa.NET.ImGui.ImGui.EndMainMenuBar();
            else Hexa.NET.ImGui.ImGui.EndMenuBar();
        }
        else
        {
            foreach (var child in Items)
                child.UpdateEvents();
        }

        if (fontPtr != null)
            Hexa.NET.ImGui.ImGui.PopFont();
    }

    private int GetHeight()
    {
        ImFontPtr? fontPtr = Font?.GetPointer();
        if (fontPtr != null)
            Hexa.NET.ImGui.ImGui.PushFont(fontPtr.Value, Font!.Data.Size);

        var height = Items.Count > 0
            ? Items.Max(x => x.Height) :
            // HINT: It's currently unknown where those 3 pixels come from, but they have to be added to get the correct size of the menu
            (int)(TextMeasurer.GetCurrentLineHeight() + Hexa.NET.ImGui.ImGui.GetStyle().FramePadding.Y * 2) + 3;

        if (fontPtr != null)
            Hexa.NET.ImGui.ImGui.PopFont();

        return height;
    }
}