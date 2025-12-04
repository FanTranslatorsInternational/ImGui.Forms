using System;
using System.Collections.Generic;
using ImGui.Forms.Factories;
using ImGuiNET;

namespace ImGui.Forms.Controls.Menu;

public class ContextMenu
{
    private bool _isOpen;

    /// <summary>
    /// The Id for this component.
    /// </summary>
    public int Id => IdFactory.Get(this);

    public IList<MenuBarItem> Items { get; } = new List<MenuBarItem>();

    #region Events

    public event EventHandler Show;

    #endregion

    public void Update()
    {
        ImGuiNET.ImGui.PushID(Id);

        var isOpenLocal = false;
        if (ImGuiNET.ImGui.BeginPopupContextItem(Id.ToString(), ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight))
        {
            isOpenLocal = true;

            if (!_isOpen)
            {
                _isOpen = true;
                OnShow();
            }

            foreach (var item in Items)
                item.Update();

            ImGuiNET.ImGui.EndPopup();
        }

        if (_isOpen && !isOpenLocal)
            _isOpen = false;

        ImGuiNET.ImGui.PopID();
    }

    private void OnShow()
    {
        Show?.Invoke(this, new EventArgs());
    }
}