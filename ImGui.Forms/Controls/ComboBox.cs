using System;
using System.Collections.Generic;
using System.Linq;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class ComboBox<TItem> : Component
    {
        public IList<ComboBoxItem<TItem>> Items { get; } = new List<ComboBoxItem<TItem>>();

        public ComboBoxItem<TItem> SelectedItem { get; set; }

        public SizeValue Width { get; set; } = SizeValue.Content;

        #region Events

        public event EventHandler SelectedItemChanged;

        #endregion

        public override Size GetSize()
        {
            var maxWidth = Items.Select(x => FontResource.GetCurrentLineWidth(x.Name)).DefaultIfEmpty(0).Max() + (int)ImGuiNET.ImGui.GetStyle().ItemInnerSpacing.X * 2;
            var arrowWidth = 20;

            SizeValue width = Width.IsContentAligned ? maxWidth + arrowWidth : Width;
            var height = FontResource.GetCurrentLineHeight() + (int)ImGuiNET.ImGui.GetStyle().ItemInnerSpacing.Y * 2;

            return new Size(width, height);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            ImGuiNET.ImGui.SetNextItemWidth(contentRect.Width);

            var selectedName = SelectedItem?.Name ?? string.Empty;
            if (ImGuiNET.ImGui.BeginCombo($"##combo{Id}", selectedName))
            {
                for (var i = 0; i < Items.Count; i++)
                {
                    if (ImGuiNET.ImGui.Selectable(Items[i].Name, SelectedItem == Items[i]))
                    {
                        var hasChanged = SelectedItem != Items[i];
                        SelectedItem = Items[i];

                        if (hasChanged)
                            OnSelectedItemChanged();
                    }
                }

                ImGuiNET.ImGui.EndCombo();
            }
        }

        private void OnSelectedItemChanged()
        {
            SelectedItemChanged?.Invoke(this, new EventArgs());
        }
    }

    public class ComboBoxItem<TItem>
    {
        public TItem Content { get; }

        public LocalizedString Name { get; }

        public ComboBoxItem(TItem content, LocalizedString name = default)
        {
            Content = content;
            Name = name.IsEmpty ? (LocalizedString)content.ToString() : name;
        }

        public static implicit operator ComboBoxItem<TItem>(TItem o) => new ComboBoxItem<TItem>(o);
    }
}
