using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls.Layouts
{
    public class TableLayout : Component
    {
        private readonly ObservableList<TableRow> _rows = new ObservableList<TableRow>();

        #region Properties

        public IList<TableRow> Rows => _rows;

        public Vector2 Spacing { get; set; }

        public Size Size { get; set; } = Size.Parent;

        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }

        #endregion

        public TableLayout()
        {
            _rows.ItemAdded += Rows_ItemAdded;
            _rows.ItemRemoved += Rows_ItemRemoved;
            _rows.ItemSet += _rows_ItemSet;
            _rows.ItemInserted += _rows_ItemInserted;
        }

        public override Size GetSize() => Size;

        protected override int GetContentWidth(int parentWidth, float layoutCorrection = 1f)
        {
            var widths = GetColumnWidths(parentWidth, layoutCorrection);
            return Math.Min(parentWidth, widths.Sum(x => x) + (widths.Length - 1) * (int)Spacing.X);
        }

        protected override int GetContentHeight(int parentHeight, float layoutCorrection = 1)
        {
            var heights = GetRowHeights(parentHeight, layoutCorrection);
            return Math.Min(parentHeight, heights.Sum(x => x) + (heights.Length - 1) * (int)Spacing.Y);
        }

        public IEnumerable<TableCell> GetCellsByRow(int row)
        {
            if (row < 0 || row >= Rows.Count)
                return Array.Empty<TableCell>();

            return Rows[row].Cells.ToArray();
        }

        public IEnumerable<TableCell> GetCellsByColumn(int col)
        {
            if (col < 0 || col >= GetMaxColumnCount())
                return Array.Empty<TableCell>();

            return Rows.Select(x => x.Cells.Count <= col ? null : x.Cells[col]);
        }

        public TableCell GetCell(int row, int col)
        {
            var rows = GetCellsByRow(row).ToArray();
            if (rows.Length <= col)
                return null;

            return rows[col];
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            if (ImGuiNET.ImGui.BeginChild($"{Id}", contentRect.Size, false, ImGuiWindowFlags.NoScrollbar))
            {
                var localWidths = GetColumnWidths(contentRect.Width, 1f);
                var localHeights = GetRowHeights(contentRect.Height, 1f);

                var (x, y) = GetInitPoint(localWidths, localHeights, contentRect);
                var origX = x;

                var localCells = Rows.Select(r => r.Cells).ToArray();
                var localMaxColumns = GetMaxColumnCount();

                for (var r = 0; r < localCells.Length; r++)
                {
                    var row = localCells[r];
                    var cellHeight = localHeights[r];

                    for (var c = 0; c < localMaxColumns; c++)
                    {
                        var cell = c < row.Count ? row[c] : null;
                        var cellWidth = localWidths[c];

                        // Apply cell alignment
                        var cellInternalSize = cell?.Content?.GetSize() ?? Size.Parent;
                        var cellInternalWidth = cellInternalSize.Width.IsAbsolute ? cell?.Content?.GetWidth(cellWidth) ?? 0 : cellWidth;
                        var cellInternalHeight = cellInternalSize.Height.IsAbsolute ? cell?.Content?.GetHeight(cellHeight) ?? 0 : cellHeight;
                        var xAdjust = 0f;
                        var yAdjust = 0f;

                        switch (cell?.HorizontalAlignment ?? HorizontalAlignment.Left)
                        {
                            case HorizontalAlignment.Center:
                                xAdjust = (cellWidth - cellInternalWidth) / 2f;
                                break;

                            case HorizontalAlignment.Right:
                                xAdjust = cellWidth - cellInternalWidth;
                                break;
                        }

                        switch (cell?.VerticalAlignment ?? VerticalAlignment.Top)
                        {
                            case VerticalAlignment.Center:
                                yAdjust = (cellHeight - cellInternalHeight) / 2f;
                                break;

                            case VerticalAlignment.Bottom:
                                yAdjust = cellHeight - cellInternalHeight;
                                break;
                        }

                        // Rendering
                        // HINT: Make child container as big as the component returned
                        if (cell != null && cellWidth > 0 && cellHeight > 0)
                        {
                            // Draw cell border
                            if (cell.ShowBorder)
                                ImGuiNET.ImGui.GetWindowDrawList().AddRect(new Vector2(x, y), new Vector2(x + cellWidth, y + cellHeight), Style.GetColor(ImGuiCol.Border).ToUInt32(), 0);

                            // Determine scroll position
                            var sx = ImGuiNET.ImGui.GetScrollX();
                            var sy = ImGuiNET.ImGui.GetScrollY();

                            // Draw cell container
                            ImGuiNET.ImGui.SetCursorPosX(x);
                            ImGuiNET.ImGui.SetCursorPosY(y);

                            if (ImGuiNET.ImGui.BeginChild($"{Id}-{r}-{c}", new Vector2(cellWidth, cellHeight), false, ImGuiWindowFlags.NoScrollbar))
                            {
                                // Draw cell content container
                                ImGuiNET.ImGui.SetCursorPosX(xAdjust);
                                ImGuiNET.ImGui.SetCursorPosY(yAdjust);

                                if (ImGuiNET.ImGui.BeginChild($"{Id}-{r}-{c}-content", new Vector2(cellInternalWidth, cellInternalHeight), false, ImGuiWindowFlags.NoScrollbar))
                                {
                                    // Draw component
                                    cell.Content?.Update(new Rectangle((int)(contentRect.X + x + xAdjust - sx), (int)(contentRect.Y + y + yAdjust - sy), cellInternalWidth, cellInternalHeight));
                                }

                                ImGuiNET.ImGui.EndChild();
                            }

                            ImGuiNET.ImGui.EndChild();
                        }

                        x += cellWidth + (cellWidth <= 0 ? 0 : Spacing.X);
                    }

                    x = origX;
                    y += cellHeight + (cellHeight <= 0 ? 0 : Spacing.Y);
                }
            }

            ImGuiNET.ImGui.EndChild();
        }

        #region Width calculation

        private int[] GetColumnWidths(int componentWidth, float layoutCorrection)
        {
            var maxColumnCount = GetMaxColumnCount();
            var result = Enumerable.Repeat(-1, maxColumnCount).ToArray();

            var availableWidth = (int)(componentWidth * layoutCorrection - Math.Max(0, GetValidColumnCount() - 1) * Spacing.X);
            var maxRelatives = Enumerable.Range(0, maxColumnCount).Select(GetMaxRelativeWidth).ToArray();

            // Preset columns with only static widths
            for (var c = 0; c < maxColumnCount; c++)
            {
                var cells = GetCellsByColumn(c).ToArray();
                if (cells.All(x => x?.Size.Width.IsAbsolute ?? true))
                {
                    var maxCellWidth = 0;
                    foreach (var cell in cells)
                    {
                        if (cell?.Content == null) continue;

                        var widthValue = (int)cell.Size.Width.Value;

                        var maxValue = widthValue < 0 ?
                            cell.Content.GetWidth(componentWidth, layoutCorrection) :
                            widthValue;
                        maxValue = Math.Min(availableWidth, maxValue);

                        if (maxValue > maxCellWidth)
                            maxCellWidth = maxValue;
                    }

                    availableWidth -= maxCellWidth;
                    result[c] = maxCellWidth;
                }
            }

            // Set column widths with absolute and relative widths
            var widthCorrection = 1f / (maxRelatives.Sum() <= 1f ? 1f : maxRelatives.Sum());
            for (var c = 0; c < maxColumnCount; c++)
            {
                // Skip column, if its width is already set
                if (result[c] != -1)
                    continue;

                // Skip column, if all have relative width
                var cells = GetCellsByColumn(c).ToArray();
                if (cells.All(x => !x?.Size.Width.IsAbsolute ?? false))
                    continue;

                var maxIsAbsolute = true;
                var maxCellWidth = 0;
                foreach (var cell in cells)
                {
                    if (cell?.Content == null)
                        continue;

                    var cellWidth = cell.Size.Width;
                    if (cellWidth.IsAbsolute)
                    {
                        maxIsAbsolute = true;

                        var maxValue = cellWidth.Value < 0 ?
                            cell.GetWidth(componentWidth, layoutCorrection) :
                            (int)cellWidth.Value;

                        if (maxValue > maxCellWidth)
                            maxCellWidth = Math.Min(availableWidth, maxValue);

                        continue;
                    }

                    maxIsAbsolute = false;
                    maxCellWidth = cell.GetWidth(availableWidth, widthCorrection);
                }

                // If max width is not absolute, do nothing
                if (!maxIsAbsolute)
                    continue;

                // Otherwise, adjust layout correction and width result
                maxRelatives[c] = 0f;

                widthCorrection = 1f / (maxRelatives.Sum() <= 1f ? 1f : maxRelatives.Sum());
                availableWidth -= maxCellWidth;

                result[c] = maxCellWidth;
            }

            // Finally resolve all relative widths
            for (var c = 0; c < maxColumnCount; c++)
            {
                // Skip column, if it doesn't have any relative width anymore
                if (maxRelatives[c] == 0)
                    continue;

                result[c] = (int)(availableWidth * maxRelatives[c] * widthCorrection);
            }

            return result;
        }

        private float GetMaxRelativeWidth(int column)
        {
            var cells = GetCellsByColumn(column);
            return cells.Select(x => x?.Size.Width ?? 0).Where(x => !x.IsAbsolute).DefaultIfEmpty(0f).Max(x => x.Value);
        }

        #endregion

        #region Height calculation

        private int[] GetRowHeights(int componentHeight, float layoutCorrection)
        {
            var result = Enumerable.Repeat(-1, Rows.Count).ToArray();

            var availableHeight = (int)(componentHeight * layoutCorrection - Math.Max(0, GetValidRowCount() - 1) * Spacing.Y);
            var maxRelatives = Enumerable.Range(0, Rows.Count).Select(GetMaxRelativeHeight).ToArray();

            // Preset columns with only static heights
            for (var r = 0; r < Rows.Count; r++)
            {
                var cells = GetCellsByRow(r).ToArray();
                if (cells.All(x => x?.Size.Height.IsAbsolute ?? true))
                {
                    var maxCellHeight = 0;
                    foreach (var cell in cells)
                    {
                        if (cell?.Content == null) continue;

                        var maxValue = cell.Size.Height.IsContentAligned ?
                            cell.Content.GetHeight(componentHeight, layoutCorrection) :
                            (int)cell.Size.Height.Value;
                        maxValue = Math.Min(availableHeight, maxValue);

                        if (maxValue > maxCellHeight)
                            maxCellHeight = maxValue;
                    }

                    availableHeight -= maxCellHeight;
                    result[r] = maxCellHeight;
                }
            }

            // Set column heights with absolute and relative heights
            var heightCorrection = 1f / (maxRelatives.Sum() <= 1f ? 1f : maxRelatives.Sum());
            for (var r = 0; r < Rows.Count; r++)
            {
                // Skip row, if its height is already set
                if (result[r] != -1)
                    continue;

                // Skip row, if all have relative height
                var cells = GetCellsByRow(r).ToArray();
                if (cells.All(x => !x?.Size.Height.IsAbsolute ?? false))
                    continue;

                var maxIsAbsolute = true;
                var maxCellHeight = 0;
                foreach (var cell in cells)
                {
                    if (cell?.Content == null)
                        continue;

                    var cellHeight = cell.Size.Height;
                    if (cellHeight.IsAbsolute)
                    {
                        maxIsAbsolute = true;

                        var maxValue = cellHeight.Value < 0 ?
                            cell.GetHeight(componentHeight, layoutCorrection) :
                            cellHeight.Value;
                        maxValue = Math.Min(availableHeight, maxValue);

                        if (maxValue > maxCellHeight)
                            maxCellHeight = (int)maxValue;

                        continue;
                    }

                    maxIsAbsolute = false;
                    maxCellHeight = cell.GetHeight(availableHeight, heightCorrection);
                }

                // If max height is not absolute, do nothing
                if (!maxIsAbsolute)
                    continue;

                // Otherwise, adjust layout correction and height result
                maxRelatives[r] = 0f;

                heightCorrection = 1f / (maxRelatives.Sum() <= 1f ? 1f : maxRelatives.Sum());
                availableHeight -= maxCellHeight;

                result[r] = maxCellHeight;
            }

            // Finally resolve all relative heights
            for (var c = 0; c < Rows.Count; c++)
            {
                // Skip column, if it doesn't have any relative width anymore
                if (maxRelatives[c] == 0)
                    continue;

                result[c] = (int)(availableHeight * maxRelatives[c] * heightCorrection);
            }

            return result;
        }

        private float GetMaxRelativeHeight(int row)
        {
            var cells = GetCellsByRow(row);
            return cells.Select(x => x?.Size.Height ?? 0).Where(x => !x.IsAbsolute).DefaultIfEmpty(0f).Max(x => x.Value);
        }

        #endregion

        #region Support

        private (float, float) GetInitPoint(int[] widths, int[] heights, Rectangle contentRect)
        {
            var totalWidth = widths.Sum(x => x) + Math.Max(0, widths.Length - 1) * (int)Spacing.X;
            var totalHeight = heights.Sum(x => x) + Math.Max(0, heights.Length - 1) * (int)Spacing.Y;

            var addX = 0;
            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    addX = (contentRect.Width - totalWidth) / 2;
                    break;

                case HorizontalAlignment.Right:
                    addX = contentRect.Width - totalWidth;
                    break;
            }

            var addY = 0;
            switch (VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    addY = (contentRect.Height - totalHeight) / 2;
                    break;

                case VerticalAlignment.Bottom:
                    addY = contentRect.Height - totalHeight;
                    break;
            }

            var x = ImGuiNET.ImGui.GetCursorPosX() + addX;
            var y = ImGuiNET.ImGui.GetCursorPosY() + addY;

            return (x, y);
        }

        private int GetMaxColumnCount()
        {
            if (Rows.Count == 0)
                return 0;

            return Rows.Max(x => x.Cells.Count);
        }

        private int GetValidColumnCount()
        {
            var res = 0;
            for (var i = 0; i < GetMaxColumnCount(); i++)
            {
                var cells = GetCellsByColumn(i).ToArray();
                if (cells.Any(x => x?.Content != null && x.Content.Visible || (x?.HasSize ?? false)))
                    if (cells.All(x => x?.Size.IsVisible ?? true))
                        res++;
            }

            return res;
        }

        private int GetValidRowCount()
        {
            var res = 0;
            foreach (var row in Rows)
            {
                if (row.Cells.Any(x => x?.Content != null && x.Content.Visible || (x?.HasSize ?? false)))
                    if (row.Cells.All(x => x?.Size.IsVisible ?? false))
                        res++;
            }

            return res;
        }

        #endregion

        #region Event Methods

        private void Rows_ItemAdded(object sender, ItemEventArgs<TableRow> e)
        {
            e.Item._parent = this;
        }

        private void Rows_ItemRemoved(object sender, ItemEventArgs<TableRow> e)
        {
            e.Item._parent = null;
        }

        private void _rows_ItemSet(object sender, ItemEventArgs<TableRow> e)
        {
            e.Item._parent = this;
        }

        private void _rows_ItemInserted(object sender, ItemEventArgs<TableRow> e)
        {
            e.Item._parent = this;
        }

        #endregion

        #region Cell Event Methods

        internal void Cells_ItemAdded()
        {
        }

        internal void Cells_ItemRemoved()
        {
        }

        internal void Cells_ItemInserted()
        {
        }

        internal void Cells_ItemSet()
        {
        }

        #endregion
    }
}
