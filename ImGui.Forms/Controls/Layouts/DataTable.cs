using System;
using System.Collections.Generic;
using System.Linq;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls.Layouts
{
    public class DataTable<TData> : Component
    {
        private (int, int) _clickedCell = (-1, -1);
        private readonly IList<int> _selectedIndexes = new List<int>();

        private IList<TData> _rows;

        public IList<DataTableColumn<TData>> Columns { get; } = new List<DataTableColumn<TData>>();

        public IList<TData> Rows
        {
            get => _rows;
            set
            {
                _rows = value;

                _selectedIndexes.Clear();
                OnSelectedRowsChanged();
            }
        }

        public IEnumerable<TData> SelectedRows => _rows.Where((r, i) => _selectedIndexes.Contains(i));

        public bool IsResizable { get; set; }

        public bool IsSelectable { get; set; } = true;

        public bool ShowHeaders { get; set; } = true;

        public bool CanSelectMultiple { get; set; }

        public Size Size { get; set; } = Size.Parent;

        public ContextMenu ContextMenu { get; set; }

        #region Events

        public event EventHandler SelectedRowsChanged;
        public event EventHandler DoubleClicked;

        #endregion

        public override Size GetSize()
        {
            return Size;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var localRows = _rows ?? new List<TData>();

            var flags = ImGuiTableFlags.BordersV;
            if (IsResizable) flags |= ImGuiTableFlags.Resizable;

            if (ImGuiNET.ImGui.BeginChild($"{Id}c"))
            {
                if (ImGuiNET.ImGui.BeginTable($"{Id}t", Columns.Count, flags))
                {
                    if (ShowHeaders)
                    {
                        foreach (var column in Columns)
                            ImGuiNET.ImGui.TableSetupColumn(column.Name ?? string.Empty);
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

                            if (IsSelectable && c == 0)
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
