using System;
using System.Collections.Generic;
using System.Linq;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Menu;
using ImGuiNET;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls.Lists
{
    public class DataTable<TData> : Component
    {
        private (int, int) _clickedCell = (-1, -1);
        private readonly IList<int> _selectedIndexes = new System.Collections.Generic.List<int>();

        private IList<DataTableRow<TData>> _rows = new System.Collections.Generic.List<DataTableRow<TData>>();

        #region Properties

        public IList<DataTableColumn<TData>> Columns { get; } = new System.Collections.Generic.List<DataTableColumn<TData>>();

        public IList<DataTableRow<TData>> Rows
        {
            get => _rows;
            set
            {
                _rows = value;

                _selectedIndexes.Clear();
                OnSelectedRowsChanged();
            }
        }

        public IEnumerable<DataTableRow<TData>> SelectedRows => _rows.Where((r, i) => _selectedIndexes.Contains(i));

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

            if (ImGuiNET.ImGui.BeginChild($"{Id}c", contentRect.Size))
            {
                if (ImGuiNET.ImGui.BeginTable($"{Id}t", Columns.Count, flags))
                {
                    if (ShowHeaders)
                    {
                        foreach (var column in Columns)
                            ImGuiNET.ImGui.TableSetupColumn(column.Name);
                        ImGuiNET.ImGui.TableHeadersRow();
                    }

                    for (var r = 0; r < localRows.Count; r++)
                    {
                        var row = localRows[r];
                        var isRowSelected = _selectedIndexes.Contains(r);

                        ImGuiNET.ImGui.TableNextRow();

                        for (var c = 0; c < Columns.Count; c++)
                        {
                            var column = Columns[c];
                            ImGuiNET.ImGui.TableSetColumnIndex(c);

                            var rowColor = row.TextColor;
                            if (!rowColor.IsEmpty)
                                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Text, row.TextColor.ToUInt32());

                            if (IsSelectable && row.CanSelect && c == 0)
                            {
                                var isSelected = ImGuiNET.ImGui.Selectable(column.Get(row), isRowSelected, ImGuiSelectableFlags.SpanAllColumns);
                                isSelected |= ImGuiNET.ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGuiNET.ImGui.IsMouseClicked(ImGuiMouseButton.Right);

                                if (isSelected)
                                {
                                    if (CanSelectMultiple && ImGuiNET.ImGui.GetIO().KeyCtrl)
                                    {
                                        if (isRowSelected)
                                            _selectedIndexes.Remove(r);
                                        else
                                            _selectedIndexes.Add(r);
                                    }
                                    else
                                    {
                                        _selectedIndexes.Clear();
                                        _selectedIndexes.Add(r);
                                    }

                                    OnSelectedRowsChanged();
                                }
                            }
                            else
                            {
                                ImGuiNET.ImGui.Text(column.Get(row));
                            }

                            if (!rowColor.IsEmpty)
                                ImGuiNET.ImGui.PopStyleColor();

                            if (IsCellClicked())
                                _clickedCell = (r, c);

                            // Add context menu
                            if (IsSelectable && _clickedCell == (r, c))
                                ContextMenu?.Update();
                        }
                    }

                    // Handle double click event
                    if (ImGuiNET.ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        if (ImGuiNET.ImGui.IsMouseHoveringRect(contentRect.Position, contentRect.Position + contentRect.Size, false))
                            OnDoubleClicked();

                    ImGuiNET.ImGui.EndTable();
                }
            }

            ImGuiNET.ImGui.EndChild();
        }

        private bool IsCellClicked()
        {
            return (ImGuiNET.ImGui.IsMouseReleased(ImGuiMouseButton.Right) || ImGuiNET.ImGui.IsMouseReleased(ImGuiMouseButton.Left)) && ImGuiNET.ImGui.IsItemHovered();
        }

        private void OnSelectedRowsChanged()
        {
            SelectedRowsChanged?.Invoke(this, new EventArgs());
        }

        private void OnDoubleClicked()
        {
            DoubleClicked?.Invoke(this, new EventArgs());
        }
    }
}
