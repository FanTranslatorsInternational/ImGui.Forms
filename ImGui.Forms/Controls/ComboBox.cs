using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

// Initial code: https://github.com/ocornut/imgui/issues/2057

namespace ImGui.Forms.Controls
{
    public class ComboBox<TItem> : Component
    {
        private string _input = string.Empty;

        public IList<ComboBoxItem<TItem>> Items { get; } = new List<ComboBoxItem<TItem>>();

        public ComboBoxItem<TItem> SelectedItem { get; set; }

        public SizeValue Width { get; set; } = SizeValue.Content;

        /// <summary>
        /// Get or set the max count of characters allowed in the input.
        /// </summary>
        public uint MaxCharacters { get; set; } = 256;

        /// <summary>
        /// Get or set the max count of items in the drop down.
        /// </summary>
        public uint MaxShowItems { get; set; } = 10;

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

        protected override unsafe void UpdateInternal(Rectangle contentRect)
        {
            //Check if both strings matches
            uint maxShowItems = MaxShowItems;
            if (maxShowItems == 0)
                maxShowItems = (uint)Items.Count;

            ImGuiNET.ImGui.PushID(Id);

            bool isFinal = ImGuiNET.ImGui.InputText("##in", ref _input, MaxCharacters, ImGuiInputTextFlags.CallbackAlways | ImGuiInputTextFlags.EnterReturnsTrue, Propose);
            if (isFinal)
            {
                ComboBoxItem<TItem> selectedItem = Items.FirstOrDefault(i => i.Name == _input);
                if (SelectedItem != selectedItem)
                {
                    SelectedItem = selectedItem;
                    OnSelectedItemChanged();
                }
            }

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
            ImGuiNET.ImGui.SetNextWindowPos(pos);
            ImGuiNET.ImGui.SetNextWindowSize(size);
            if (ImGuiNET.ImGui.BeginPopup("combobox", ImGuiWindowFlags.NoMove))
            {
                ImGuiNET.ImGui.Text("Select an item");
                ImGuiNET.ImGui.Separator();
                foreach (ComboBoxItem<TItem> item in Items)
                {
                    if (!ImGuiNET.ImGui.Selectable(item.Name)) 
                        continue;

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
        }

        private void OnSelectedItemChanged()
        {
            SelectedItemChanged?.Invoke(this, EventArgs.Empty);
        }

        private bool Identical(string buf, string item)
        {
            //Check if the item length is shorter or equal --> exclude
            if (buf.Length >= item.Length)
                return false;

            for (var i = 0; i < buf.Length; ++i)
                // set the current pos if matching or return the pos if not
                if (buf[i] != item[i])
                    return false;

            // Complete match
            // and the item size is greater --> include
            return true;
        }

        private unsafe int Propose(ImGuiInputTextCallbackData* data)
        {
            var dataPtr = new ImGuiInputTextCallbackDataPtr(data);

            // We don't want to "preselect" anything
            if (dataPtr.BufTextLen == 0)
                return 0;

            // We need to give the user a chance to remove wrong input
            if (dataPtr.EventKey == ImGuiKey.Backspace)
            {
                // We delete the last char automatically, since it is what the user wants to delete, but only if there is something (selected/marked/hovered)
                // FIXME: This worked fine, when not used as helper function
                if (data->SelectionEnd == data->SelectionStart)
                    return 0;

                if (data->BufTextLen <= 0)
                    return 0; //...and the buffer isn't empty

                if (data->CursorPos > 0) //...and the cursor not at pos 0
                    dataPtr.DeleteChars(data->CursorPos - 1, 1);

                return 0;
            }

            if (dataPtr.EventKey == ImGuiKey.Delete)
                return 0;

            string bufferString = Marshal.PtrToStringUTF8(dataPtr.Buf);
            foreach (ComboBoxItem<TItem> item in Items)
            {
                if (!Identical(bufferString, item.Name))
                    continue;

                int cursor = data->CursorPos;

                //Insert the first match
                dataPtr.DeleteChars(0, data->BufTextLen);
                dataPtr.InsertChars(0, item.Name);

                //Reset the cursor position
                data->CursorPos = cursor;

                //Select the text, so the user can simply go on writing
                data->SelectionStart = cursor;
                data->SelectionEnd = data->BufTextLen;

                break;
            }

            return 0;
        }
    }
}
