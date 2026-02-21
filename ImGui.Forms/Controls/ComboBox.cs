using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using SixLabors.ImageSharp;
using Rectangle = ImGui.Forms.Support.Rectangle;
using Size = ImGui.Forms.Models.Size;

// Initial code: https://github.com/ocornut/imgui/issues/2057

namespace ImGui.Forms.Controls;

public class ComboBox<TItem> : Component
{
    private const int ButtonSizeX_ = 11;

    private string _input = string.Empty;
    private DropDownItem<TItem> _selected;

    #region Properties

    public IList<DropDownItem<TItem>> Items { get; } = new List<DropDownItem<TItem>>();

    public IList<DropDownItem<TItem>> PreferredItems { get; } = new List<DropDownItem<TItem>>();

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

    /// <summary>
    /// Get or set the alignment of the drop down.
    /// </summary>
    public ComboBoxAlignment Alignment { get; set; } = ComboBoxAlignment.Auto;

    #endregion

    #region Events

    public event EventHandler SelectedItemChanged;

    #endregion

    public override Size GetSize()
    {
        var maxWidth = (int)(Items.Select(x => TextMeasurer.GetCurrentLineWidth(x.Name)).DefaultIfEmpty(0).Max() + Hexa.NET.ImGui.ImGui.GetStyle().ItemInnerSpacing.X * 2);
        int arrowWidth = (int)(ButtonSizeX_ + Hexa.NET.ImGui.ImGui.GetStyle().FramePadding.X * 2);

        SizeValue width = Width.IsContentAligned ? maxWidth + arrowWidth : Width;
        var height = (int)(TextMeasurer.GetCurrentLineHeight() + Hexa.NET.ImGui.ImGui.GetStyle().ItemInnerSpacing.Y * 2);

        return new Size(width, height);
    }

    protected override unsafe void UpdateInternal(Rectangle contentRect)
    {
        var enabled = Enabled;
        ApplyStyles(enabled);

        //Check if both strings matches
        uint maxShowItems;
        if (MaxShowItems <= 0)
            maxShowItems = (uint)Items.Count;
        else
            maxShowItems = (uint)Math.Min(MaxShowItems, Items.Count);

        Hexa.NET.ImGui.ImGui.PushID(Id);

        var arrowWidth = ButtonSizeX_ + Hexa.NET.ImGui.ImGui.GetStyle().FramePadding.X * 2;
        Hexa.NET.ImGui.ImGui.SetNextItemWidth(contentRect.Width - arrowWidth);

        var localInput = _input;

        bool isFinal = Hexa.NET.ImGui.ImGui.InputText("##in", ref localInput, MaxCharacters, ImGuiInputTextFlags.CallbackAlways | ImGuiInputTextFlags.EnterReturnsTrue, Propose);
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

        Hexa.NET.ImGui.ImGui.OpenPopupOnItemClick("combobox"); // Enable right-click
        Vector2 pos = Hexa.NET.ImGui.ImGui.GetItemRectMin();
        Vector2 size = Hexa.NET.ImGui.ImGui.GetItemRectSize();

        Hexa.NET.ImGui.ImGui.SameLine(0, 0);
        if (Hexa.NET.ImGui.ImGui.ArrowButton("##openCombo", ImGuiDir.Down))
        {
            Hexa.NET.ImGui.ImGui.OpenPopup("combobox");
        }
        Hexa.NET.ImGui.ImGui.OpenPopupOnItemClick("combobox"); // Enable right-click

        Vector2 arrowSize = Hexa.NET.ImGui.ImGui.GetItemRectSize();
        float itemSize = TextMeasurer.GetCurrentLineHeight() + Style.GetStyleVector2(ImGuiStyleVar.ItemSpacing).Y;

        Vector2 popupPos = pos;
        Vector2 popupSize = new Vector2(size.X + arrowSize.X,
            Style.GetStyleVector2(ImGuiStyleVar.WindowPadding).Y + itemSize * maxShowItems);

        switch (Alignment)
        {
            case ComboBoxAlignment.Auto:
                if (popupPos.Y + size.Y + popupSize.Y > Application.Instance.MainForm.Height)
                    popupPos.Y -= popupSize.Y;
                else
                    popupPos.Y += size.Y;

                break;

            case ComboBoxAlignment.Bottom:
                popupPos.Y += size.Y;
                break;

            case ComboBoxAlignment.Top:
                popupPos.Y -= popupSize.Y;
                break;

            default:
                throw new InvalidOperationException($"Invalid combobox alignment {Alignment}.");
        }

        Hexa.NET.ImGui.ImGui.SetNextWindowPos(popupPos);
        Hexa.NET.ImGui.ImGui.SetNextWindowSize(popupSize);

        if (enabled && Hexa.NET.ImGui.ImGui.BeginPopup("combobox", ImGuiWindowFlags.NoMove))
        {
            Vector2 itemPos = popupPos;
            for (var i = 0; i < Items.Count; i++)
            {
                DropDownItem<TItem> item = Items[i];
                bool isPreferred = PreferredItems.Contains(item);

                // Set selectable with item name
                Hexa.NET.ImGui.ImGui.PushID($"{Id}_item{i}");

                bool isSelected = Hexa.NET.ImGui.ImGui.Selectable(item.Name);
                if (isPreferred)
                {
                    var markerPos = new Vector2(size.X + arrowSize.X / 2, arrowSize.Y / 2);
                    Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddCircleFilled(itemPos + markerPos, 3f, Color.White.ToUInt32());
                }

                itemPos += size with { X = 0 };
                if (!isSelected)
                    continue;

                Hexa.NET.ImGui.ImGui.PopID();

                _input = item.Name;

                if (SelectedItem != item)
                {
                    SelectedItem = item;
                    OnSelectedItemChanged();
                }
            }

            Hexa.NET.ImGui.ImGui.EndPopup();
        }

        Hexa.NET.ImGui.ImGui.PopID();

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
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.Button, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.ButtonActive, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));

            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.FrameBg, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.TextDisabled));
        }
    }

    private void RemoveStyles(bool enabled)
    {
        if (!enabled)
            Hexa.NET.ImGui.ImGui.PopStyleColor(6);
    }

    private string? _prevBuffer;
    private string? _prevMatch;

    private unsafe int Propose(ImGuiInputTextCallbackData* data)
    {
        var dataPtr = new ImGuiInputTextCallbackDataPtr(data);
        string bufferString = Marshal.PtrToStringUTF8((nint)dataPtr.Buf);
        if (bufferString == null)
            return 0;

        if (dataPtr.SelectionEnd != dataPtr.SelectionStart && bufferString[..dataPtr.SelectionStart] == _prevBuffer)
            return 0;

        // We don't want to "preselect" anything
        if (dataPtr.BufTextLen == 0)
            return 0;

        // We need to give the user a chance to remove wrong input
        if (Hexa.NET.ImGui.ImGui.IsKeyPressed(ImGuiKey.Backspace))
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

        if (Hexa.NET.ImGui.ImGui.IsKeyPressed(ImGuiKey.Delete))
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

public enum ComboBoxAlignment
{
    Auto,
    Top,
    Bottom
}