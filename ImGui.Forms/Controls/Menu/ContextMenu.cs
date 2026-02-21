using System;
using System.Collections.Generic;
using Hexa.NET.ImGui;

namespace ImGui.Forms.Controls.Menu;

public class ContextMenu
{
    private bool _isOpen;

    /// <summary>
    /// The Id for this component.
    /// </summary>
    public int Id => Application.Instance.Ids.Get(this);

    public IList<MenuBarItem> Items { get; } = new List<MenuBarItem>();

    #region Events

    public event EventHandler Show;

    #endregion

    public void Update()
    {
        Hexa.NET.ImGui.ImGui.PushID(Id);

        var isOpenLocal = false;
        if (Hexa.NET.ImGui.ImGui.BeginPopupContextItem(Id.ToString(), ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight))
        {
            isOpenLocal = true;

            if (!_isOpen)
            {
                _isOpen = true;
                OnShow();
            }

            foreach (var item in Items)
                item.Update();

            Hexa.NET.ImGui.ImGui.EndPopup();
        }

        if (_isOpen && !isOpenLocal)
            _isOpen = false;

        Hexa.NET.ImGui.ImGui.PopID();
    }

    private void OnShow()
    {
        Show?.Invoke(this, new EventArgs());
    }
}