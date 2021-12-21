using System.Collections.Generic;
using ImGuiNET;

namespace ImGui.Forms.Controls.Menu
{
    public class ContextMenu
    {
        /// <summary>
        /// The Id for this component.
        /// </summary>
        public int Id => Application.Instance.IdFactory.Get(this);

        public IList<MenuBarItem> Items { get; } = new List<MenuBarItem>();

        public void Update()
        {
            ImGuiNET.ImGui.PushID(Id);

            if (ImGuiNET.ImGui.BeginPopupContextItem(Id.ToString(), ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight))
            {
                foreach (var item in Items)
                    item.Update();

                ImGuiNET.ImGui.EndPopup();
            }

            ImGuiNET.ImGui.PopID();
        }
    }
}
