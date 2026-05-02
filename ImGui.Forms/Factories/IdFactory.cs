using System.Runtime.CompilerServices;

namespace ImGui.Forms.Factories;

public class IdFactory
{
    private static int _counter;

    private readonly ConditionalWeakTable<object, IdHolder> _lookup = new();

    public int Get(object item)
    {
        if (_lookup.TryGetValue(item, out IdHolder? idHolder))
            return idHolder.Id;

        int id = _counter++;

        _lookup.Add(item, new IdHolder(id));

        return id;
    }

    private record IdHolder(int Id);
}