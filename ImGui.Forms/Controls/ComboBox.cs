using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

// Initial code: https://github.com/ocornut/imgui/issues/2057

namespace ImGui.Forms.Controls
{
    public class ComboBox<TItem> : Component
    {
        private const int ButtonSizeX_ = 11;

        private string _input = string.Empty;
        private DropDownItem<TItem> _selected;

        #region Properties

        public IList<DropDownItem<TItem>> Items { get; } = new List<DropDownItem<TItem>>();

        public DropDownItem<TItem> SelectedItem
        {
            get => _selected;
            set
            {
                _selected = value;
                _input = value?.Name ?? string.Empty;
            }
        }

        public SizeValue Width { get; set; } = SizeValue.Content;

        /// <summary>
        /// Get or set the max count of characters allowed in the input.
        /// </summary>
        public uint MaxCharacters { get; set; } = 256;

        /// <summary>
        /// Get or set the max count of items in the drop down.
        /// </summary>
        public uint MaxShowItems { get; set; } = 10;

        #endregion

        #region Events

        public event EventHandler SelectedItemChanged;

        #endregion

        public override Size GetSize()
        {
            var maxWidth = Items.Select(x => TextMeasurer.GetCurrentLineWidth(x.Name)).DefaultIfEmpty(0).Max() + (int)ImGuiNET.ImGui.GetStyle().ItemInnerSpacing.X * 2;
            int arrowWidth = (int)(ButtonSizeX_ + ImGuiNET.ImGui.GetStyle().FramePadding.X * 2);

            SizeValue width = Width.IsContentAligned ? maxWidth + arrowWidth : Width;
            var height = TextMeasurer.GetCurrentLineHeight() + (int)ImGuiNET.ImGui.GetStyle().ItemInnerSpacing.Y * 2;

            return new Size(width, height);
        }

        protected override unsafe void UpdateInternal(Rectangle contentRect)
        {
            var enabled = Enabled;
            ApplyStyles(enabled);

            //Check if both strings matches
            uint maxShowItems = MaxShowItems;
            if (maxShowItems == 0)
                maxShowItems = (uint)Items.Count;

            ImGuiNET.ImGui.PushID(Id);

            var arrowWidth = ButtonSizeX_ + ImGuiNET.ImGui.GetStyle().FramePadding.X * 2;
            ImGuiNET.ImGui.SetNextItemWidth(contentRect.Width - arrowWidth);

            var localInput = _input;

            bool isFinal = ImGuiNET.ImGui.InputText("##in", ref localInput, MaxCharacters, ImGuiInputTextFlags.CallbackAlways | ImGuiInputTextFlags.EnterReturnsTrue, Propose);
            if (enabled && isFinal)
            {
                DropDownItem<TItem> selectedItem = Items.FirstOrDefault(i => i.Name == localInput);
                if (SelectedItem != selectedItem)
                {
                    SelectedItem = selectedItem;
                    OnSelectedItemChanged();
                }
            }

            // Revert text changes, if not enabled
            if (enabled)
                _input = localInput;

            ImGuiNET.ImGui.OpenPopupOnItemClick("combobox"); // Enable right-click
            Vector2 pos = ImGuiNET.ImGui.GetItemRectMin();
            Vector2 size = ImGuiNET.ImGui.GetItemRectSize();

            ImGuiNET.ImGui.SameLine(0, 0);
            if (ImGuiNET.ImGui.ArrowButton("##openCombo", ImGuiDir.Down))
            {
                ImGuiNET.ImGui.OpenPopup("combobox");
            }
            ImGuiNET.ImGui.OpenPopupOnItemClick("combobox"); // Enable right-click

            pos.Y += size.Y;
            size.X += ImGuiNET.ImGui.GetItemRectSize().Y;
            size.Y += 5 + size.Y * maxShowItems;

            if (pos.Y + size.Y <= Application.Instance.MainForm.Height)
                ImGuiNET.ImGui.SetNextWindowPos(pos);
            else
                ImGuiNET.ImGui.SetNextWindowPos(new Vector2(pos.X, pos.Y - size.Y - ImGuiNET.ImGui.GetItemRectSize().Y));
            ImGuiNET.ImGui.SetNextWindowSize(size);
            if (enabled && ImGuiNET.ImGui.BeginPopup("combobox", ImGuiWindowFlags.NoMove))
            {
                for (var i = 0; i < Items.Count; i++)
                {
                    DropDownItem<TItem> item = Items[i];

                    // Set selectable with item name
                    ImGuiNET.ImGui.PushID($"{Id}_item{i}");

                    if (!ImGuiNET.ImGui.Selectable(item.Name))
                        continue;

                    ImGuiNET.ImGui.PopID();

                    _input = item.Name;

                    if (SelectedItem != item)
                    {
                        SelectedItem = item;
                        OnSelectedItemChanged();
                    }
                }

                ImGuiNET.ImGui.EndPopup();
            }

            ImGuiNET.ImGui.PopID();

            RemoveStyles(enabled);
        }

        private void OnSelectedItemChanged()
        {
            SelectedItemChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyStyles(bool enabled)
        {
            if (!enabled)
            {
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));

                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            }
        }

        private void RemoveStyles(bool enabled)
        {
            if (!enabled)
                ImGuiNET.ImGui.PopStyleColor(6);
        }

        private string? _prevBuffer;
        private string? _prevMatch;

        private unsafe int Propose(ImGuiInputTextCallbackData* data)
        {
            var dataPtr = new ImGuiInputTextCallbackDataPtr(data);
            string bufferString = Marshal.PtrToStringUTF8(dataPtr.Buf);
            if (bufferString == null)
                return 0;

            if (dataPtr.SelectionEnd != dataPtr.SelectionStart && bufferString[..dataPtr.SelectionStart] == _prevBuffer)
                return 0;

            // We don't want to "preselect" anything
            if (dataPtr.BufTextLen == 0)
                return 0;

            // We need to give the user a chance to remove wrong input
            if (ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Backspace))
            {
                // We delete the last char automatically, since it is what the user wants to delete, but only if there is something (selected/marked/hovered)
                // FIXME: This worked fine, when not used as helper function
                if (_prevBuffer != bufferString)
                    return 0;

                if (dataPtr.BufTextLen <= 0)
                    return 0; //...and the buffer isn't empty

                if (dataPtr.CursorPos > 0) //...and the cursor not at pos 0
                    dataPtr.DeleteChars(dataPtr.CursorPos - 1, 1);

                return 0;
            }

            if (ImGuiNET.ImGui.IsKeyPressed(ImGuiKey.Delete))
                return 0;

            int prevDiff = -1;
            string? itemName = null;

            foreach (DropDownItem<TItem> item in Items)
            {
                if (!Identical(bufferString, item.Name, out int diff))
                    continue;

                if (prevDiff < 0 || diff < prevDiff)
                {
                    prevDiff = diff;
                    itemName = item.Name;
                }

                if (prevDiff == 0)
                    break;
            }

            _prevBuffer = bufferString;
            _prevMatch = itemName;

            if (itemName == null)
                return 0;

            int cursor = dataPtr.CursorPos;

            // Insert the first match
            dataPtr.DeleteChars(0, dataPtr.BufTextLen);
            dataPtr.InsertChars(0, itemName);

            // Reset the cursor position
            dataPtr.CursorPos = cursor;

            // Select the text, so the user can simply go on writing
            dataPtr.SelectionStart = cursor;
            dataPtr.SelectionEnd = dataPtr.BufTextLen;

            return 0;
        }

        private bool Identical(string buf, string item, out int diff)
        {
            diff = 0;

            // Check if the item length is shorter or equal --> exclude
            if (buf.Length > item.Length)
                return false;

            for (var i = 0; i < buf.Length; ++i)
                // set the current pos if matching or return the pos if not
                if (buf[i] != item[i])
                    return false;

            // Complete match
            // and the item size is greater --> include
            diff = item.Length - buf.Length;
            return true;
        }
    }

    public class DropDownItem<TItem>
    {
        public TItem Content { get; }

        public LocalizedString Name { get; }

        public DropDownItem(TItem content, LocalizedString name = default)
        {
            Content = content;
            Name = name.IsEmpty ? (LocalizedString)content.ToString() : name;
        }

        public static implicit operator DropDownItem<TItem>(TItem o) => new(o);
    }
}
