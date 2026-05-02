using ImGui.Forms.Localization;
using System;

namespace ImGui.Forms.Controls.Lists;

public class DataTableColumn<TData>
{
    private readonly Func<TData, LocalizedString> _valueGetter;

    public LocalizedString Name { get; }
    public int SortOrder { get; set; } = -1;
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    public DataTableColumn(Func<TData, LocalizedString> valueGetter, LocalizedString name = default)
    {
        Name = name;
        _valueGetter = valueGetter;
    }

    public LocalizedString Get(DataTableRow<TData> row) => _valueGetter(row.Data);
}

public enum SortDirection
{
    Ascending = 1,
    Descending = 2
}