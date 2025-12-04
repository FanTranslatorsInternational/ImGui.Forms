using System;
using ImGui.Forms.Localization;

namespace ImGui.Forms.Controls.Lists;

public class DataTableColumn<TData>
{
    private readonly Func<TData, LocalizedString> _valueGetter;

    public LocalizedString Name { get; }

    public DataTableColumn(Func<TData, LocalizedString> valueGetter, LocalizedString name = default)
    {
        Name = name;
        _valueGetter = valueGetter;
    }

    public LocalizedString Get(DataTableRow<TData> row) => _valueGetter(row.Data);
}