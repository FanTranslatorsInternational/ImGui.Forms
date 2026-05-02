using System;
using System.Collections.Generic;
using System.Linq;
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

    private readonly System.Collections.Generic.List<DataTableRow<TData>> _selectedRows = [];

    private (int, int) _clickedCell = (-1, -1);
    private int _lastSelectedRow = -1;

    private System.Collections.Generic.List<DataTableRow<TData>> _rows = [];
    private DataTableColumn<TData>[]? _sortedColumns;

    #region Properties

    public System.Collections.Generic.List<DataTableColumn<TData>> Columns { get; } = [];

    public System.Collections.Generic.List<DataTableRow<TData>> Rows
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

    public bool IsResizable { get; set; } = true;

    public bool IsSelectable { get; set; } = true;

    public bool IsSortable { get; set; } = true;

    public bool ShowHeaders { get; set; } = true;

    public bool CanSelectMultiple { get; set; } = true;

    public bool CanSortMultiple { get; set; } = true;

    public Size Size { get; set; } = Size.Parent;

    public ContextMenu ContextMenu { get; set; }

    #endregion

    #region Events

    public event EventHandler SelectedRowsChanged;
    public event EventHandler DoubleClicked;
    public event EventHandler<DataTableSortChangedEventArgs<TData>> SortChanged;

    #endregion

    public override Size GetSize() => Size;

    private bool _isInitialSort = true;

    protected override void UpdateInternal(Rectangle contentRect)
    {
        var localRows = _rows ?? [];

        var flags = ImGuiTableFlags.BordersV;
        if (IsResizable) flags |= ImGuiTableFlags.Resizable;
        if (IsSortable) flags |= ImGuiTableFlags.Sortable;
        if (CanSortMultiple) flags |= ImGuiTableFlags.SortMulti;

        if (Hexa.NET.ImGui.ImGui.BeginChild($"{Id}c", contentRect.Size))
        {
            if (Hexa.NET.ImGui.ImGui.BeginTable($"{Id}", Columns.Count, flags))
            {
                if (ShowHeaders)
                {
                    for (var i = 0; i < Columns.Count; i++)
                        Hexa.NET.ImGui.ImGui.TableSetupColumn(Columns[i].Name, 0, 0f, (uint)(Id + i + 1));

                    Hexa.NET.ImGui.ImGui.TableHeadersRow();
                }

                if (IsSortable)
                    ProcessSorting();

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

    private void ProcessSorting()
    {
        if (_isInitialSort)
        {
            ProcessInitialSort();
        }
        else
        {
            ProcessSortedSpecs();
        }
    }

    private void ProcessInitialSort()
    {
        if (Columns.All(x => x.SortOrder < 0))
        {
            _isInitialSort = false;
            return;
        }

        var isFirstColumn = true;
        foreach (DataTableColumn<TData> orderedColumn in Columns.OrderBy(x => x.SortOrder))
        {
            if (orderedColumn.SortOrder < 0)
                continue;

            ImGuiP.TableSetColumnSortDirection(Columns.IndexOf(orderedColumn), (ImGuiSortDirection)orderedColumn.SortDirection, !isFirstColumn);
            isFirstColumn = false;
        }

        _sortedColumns = Columns.Where(x => x.SortOrder >= 0).OrderBy(x => x.SortOrder).ToArray();

        SortRows(_sortedColumns);
        OnSortChanged(_sortedColumns);

        var tableSpecs = Hexa.NET.ImGui.ImGui.TableGetSortSpecs();
        if (!tableSpecs.IsNull)
        {
            tableSpecs.SpecsDirty = false;
            _isInitialSort = false;
        }
    }

    private void ProcessSortedSpecs()
    {
        var tableSpecs = Hexa.NET.ImGui.ImGui.TableGetSortSpecs();
        if (tableSpecs.IsNull || !tableSpecs.SpecsDirty)
            return;

        var columns = Columns.ToList<DataTableColumn<TData>?>();

        _sortedColumns = new DataTableColumn<TData>[tableSpecs.SpecsCount];
        for (var i = 0; i < tableSpecs.SpecsCount; i++)
        {
            var specs = tableSpecs.Specs[i];

            var columnIndex = specs.ColumnIndex;
            var column = columns[columnIndex]!;

            column.SortOrder = i;
            column.SortDirection = (SortDirection)specs.SortDirection;

            _sortedColumns[i] = column;
            columns[columnIndex] = null;
        }

        foreach (DataTableColumn<TData>? column in columns)
        {
            if (column is null)
                continue;

            column.SortOrder = -1;
            column.SortDirection = SortDirection.Ascending;
        }

        SortRows(_sortedColumns);
        OnSortChanged(_sortedColumns);

        tableSpecs.SpecsDirty = false;
    }

    private void SortRows(IReadOnlyList<DataTableColumn<TData>> sortedColumns)
    {
        if (sortedColumns.Count <= 0)
            return;

        var orderedRows = sortedColumns[0].SortDirection switch
        {
            SortDirection.Ascending => _rows.OrderBy(r => (string)sortedColumns[0].Get(r)),
            SortDirection.Descending => _rows.OrderByDescending(r => (string)sortedColumns[0].Get(r)),
            _ => throw new InvalidOperationException($"Unsupported sort direction {sortedColumns[0].SortDirection}.")
        };

        for (var i = 1; i < sortedColumns.Count; i++)
        {
            int index = i;
            orderedRows = sortedColumns[i].SortDirection switch
            {
                SortDirection.Ascending => orderedRows.ThenBy(r => (string)sortedColumns[index].Get(r)),
                SortDirection.Descending => orderedRows.ThenByDescending(r => (string)sortedColumns[index].Get(r)),
                _ => throw new InvalidOperationException($"Unsupported sort direction {sortedColumns[index].SortDirection}.")
            };
        }

        _rows = orderedRows.ToList();
    }

    private void OnSortChanged(IReadOnlyList<DataTableColumn<TData>> sortedColumns)
    {
        SortChanged?.Invoke(this, new DataTableSortChangedEventArgs<TData>(sortedColumns));
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

public class DataTableSortChangedEventArgs<TData> : EventArgs
{
    public IReadOnlyList<DataTableColumn<TData>> SortedColumns { get; }

    public DataTableSortChangedEventArgs(IReadOnlyList<DataTableColumn<TData>> sortedColumns)
    {
        SortedColumns = sortedColumns;
    }
}