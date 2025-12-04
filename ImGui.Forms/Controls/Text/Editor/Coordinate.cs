using System;

namespace ImGui.Forms.Controls.Text.Editor;

public struct Coordinate
{
    public int Line { get; set; }
    public int Column { get; set; }

    public Coordinate()
    {
        Line = -1;
        Column = -1;
    }

    public Coordinate(int line, int column)
    {
        Line = Math.Max(0, line);
        Column = Math.Max(0, column);
    }

    public static bool operator ==(Coordinate a, Coordinate o) => a.Line == o.Line && a.Column == o.Column;
    public static bool operator !=(Coordinate a, Coordinate o) => a.Line != o.Line || a.Column != o.Column;

    public static bool operator <(Coordinate a, Coordinate o)
    {
        if (a.Line != o.Line)
            return a.Line < o.Line;

        return a.Column < o.Column;
    }
    public static bool operator >(Coordinate a, Coordinate o)
    {
        if (a.Line != o.Line)
            return a.Line > o.Line;

        return a.Column > o.Column;
    }

    public static bool operator <=(Coordinate a, Coordinate o)
    {
        if (a.Line != o.Line)
            return a.Line < o.Line;

        return a.Column <= o.Column;
    }
    public static bool operator >=(Coordinate a, Coordinate o)
    {
        if (a.Line != o.Line)
            return a.Line > o.Line;

        return a.Column >= o.Column;
    }

    public bool Equals(Coordinate other)
    {
        return Line == other.Line && Column == other.Column;
    }
    public override bool Equals(object obj)
    {
        return obj is Coordinate other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Line, Column);
    }
}