using System;
using System.Collections.Generic;
using System.Text;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Extensions;
using ImGui.Forms.Localization;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;
using ImGui.Forms.Support;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls.Lists;

public class DataTable<TData> : Component
{
    private readonly KeyCommand _copyCommand = new(ImGuiKey.ModCtrl, ImGuiKey.C);

    private readonly System.Collections.Generic.List<DataTableRow<TData>> _selectedRows = new();

    private (int, int) _clickedCell = (-1, -1);
    private int _lastSelectedRow = -1;
    private float _scrollY;

    private IList<DataTableRow<TData>> _rows = new System.Collections.Generic.List<DataTableRow<TData>>();

    #region Properties

    public IList<DataTableColumn<TData>> Columns { get; } = new System.Collections.Generic.List<DataTableColumn<TData>>();

    public IList<DataTableRow<TData>> Rows
    {
        get => _rows;
        set
        {
            _rows = value;

            _selectedRows.Clear();
            OnSelectedRowsChanged();
        }
    }

    public IReadOnlyList<DataTableRow<TData>> SelectedRows => _selectedRows;

    public bool IsResizable { get; set; }

    public bool IsSelectable { get; set; } = true;

    public bool ShowHeaders { get; set; } = true;

    public bool CanSelectMultiple { get; set; }

    public Size Size { get; set; } = Size.Parent;

    public ContextMenu ContextMenu { get; set; }

    #endregion

    #region Events

    public event EventHandler SelectedRowsChanged;
    public event EventHandler DoubleClicked;

    #endregion

    public override Size GetSize() => Size;

    protected override void UpdateInternal(Rectangle contentRect)
    {
        var localRows = _rows ?? new System.Collections.Generic.List<DataTableRow<TData>>();

        var flags = ImGuiTableFlags.BordersV;
        if (IsResizable) flags |= ImGuiTableFlags.Resizable;

        if (Hexa.NET.ImGui.ImGui.BeginChild($"{Id}c", contentRect.Size))
        {
            float newScrollY = Hexa.NET.ImGui.ImGui.GetScrollY();

            if (_scrollY != newScrollY)
            {
                if (IsTabInactiveCore())
                    Hexa.NET.ImGui.ImGui.SetScrollY(_scrollY);

                _scrollY = newScrollY;
            }

            if (Hexa.NET.ImGui.ImGui.BeginTable($"{Id}t", Columns.Count, flags))
            {
                if (ShowHeaders)
                {
                    foreach (var column in Columns)
                        Hexa.NET.ImGui.ImGui.TableSetupColumn(column.Name);
                    Hexa.NET.ImGui.ImGui.TableHeadersRow();
                }

                for (var r = 0; r < localRows.Count; r++)
                {
                    var row = localRows[r];
                    var isRowSelected = _selectedRows.Contains(row);

                    Hexa.NET.ImGui.ImGui.TableNextRow();

                    for (var c = 0; c < Columns.Count; c++)
                    {
                        var column = Columns[c];
                        Hexa.NET.ImGui.ImGui.TableSetColumnIndex(c);

                        var rowColor = row.TextColor;
                        if (!rowColor.IsEmpty)
                            Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.Text, row.TextColor.ToUInt32());

                        if (IsSelectable && row.CanSelect && c == 0)
                        {
                            var isSelected = Hexa.NET.ImGui.ImGui.Selectable(column.Get(row), isRowSelected, ImGuiSelectableFlags.SpanAllColumns);
                            isSelected |= Hexa.NET.ImGui.ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup)
                                          && (Hexa.NET.ImGui.ImGui.IsMouseClicked(ImGuiMouseButton.Left) || Hexa.NET.ImGui.ImGui.IsMouseClicked(ImGuiMouseButton.Right));

                            if (isSelected)
                            {
                                if (CanSelectMultiple && Hexa.NET.ImGui.ImGui.GetIO().KeyCtrl)
                                {
                                    if (isRowSelected)
                                    {
                                        _selectedRows.Remove(row);
                                        _lastSelectedRow = -1;
                                    }
                                    else
                                    {
                                        _selectedRows.Add(row);
                                        _lastSelectedRow = r;
                                    }
                                }
                                else if (CanSelectMultiple && Hexa.NET.ImGui.ImGui.GetIO().KeyShift)
                                {
                                    if (_lastSelectedRow >= 0 && _lastSelectedRow != r)
                                    {
                                        _selectedRows.Clear();

                                        var min = Math.Clamp(Math.Min(_lastSelectedRow, r), 0, localRows.Count);
                                        var max = Math.Clamp(Math.Max(_lastSelectedRow, r), 0, localRows.Count - 1);

                                        for (var i = min; i <= max; i++)
                                            _selectedRows.Add(localRows[i]);
                                    }
                                }
                                else
                                {
                                    _selectedRows.Clear();
                                    _selectedRows.Add(row);

                                    _lastSelectedRow = r;
                                }

                                OnSelectedRowsChanged();
                            }
                        }
                        else
                        {
                            Hexa.NET.ImGui.ImGui.Text(column.Get(row));
                        }

                        if (!rowColor.IsEmpty)
                            Hexa.NET.ImGui.ImGui.PopStyleColor();

                        if (IsCellClicked())
                            _clickedCell = (r, c);

                        // Add context menu
                        if (IsSelectable && _clickedCell == (r, c))
                            ContextMenu?.Update();
                    }
                }

                Hexa.NET.ImGui.ImGui.EndTable();

                // Handle copy data
                if (_copyCommand.IsPressed() && Application.Instance.MainForm.IsActiveLayer())
                    CopySelectedRows();

                // Handle double click event
                if (Hexa.NET.ImGui.ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && Application.Instance.MainForm.IsActiveLayer())
                    OnDoubleClicked();
            }
        }

        Hexa.NET.ImGui.ImGui.EndChild();
    }

    private bool IsCellClicked()
    {
        return (Hexa.NET.ImGui.ImGui.IsMouseReleased(ImGuiMouseButton.Right) || Hexa.NET.ImGui.ImGui.IsMouseReleased(ImGuiMouseButton.Left)) && Hexa.NET.ImGui.ImGui.IsItemHovered();
    }

    private void CopySelectedRows()
    {
        if (_selectedRows.Count <= 0)
            return;

        var sb = new StringBuilder();
        var rowValues = new System.Collections.Generic.List<LocalizedString>(Columns.Count);

        foreach (var selectedRow in _selectedRows)
        {
            foreach (DataTableColumn<TData> column in Columns)
                rowValues.Add(column.Get(selectedRow));

            sb.AppendLine(string.Join('\t', rowValues));

            rowValues.Clear();
        }

        Hexa.NET.ImGui.ImGui.SetClipboardText(sb.ToString());
    }

    private void OnSelectedRowsChanged()
    {
        SelectedRowsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnDoubleClicked()
    {
        DoubleClicked?.Invoke(this, EventArgs.Empty);
    }

    protected override int GetContentHeight(int parentWidth, int parentHeight, float layoutCorrection = 1)
    {
        var height = 0f;

        float lineHeight = TextMeasurer.GetCurrentLineHeight();
        float cellPaddingY = Style.GetStyleVector2(ImGuiStyleVar.CellPadding).Y * 2;
        float cellHeight = lineHeight + cellPaddingY;

        if (ShowHeaders)
            height += cellHeight;

        height += cellHeight * _rows.Count;

        return (int)Math.Ceiling(height);
    }
}