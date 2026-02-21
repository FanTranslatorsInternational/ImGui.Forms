using System.Numerics;

namespace ImGui.Forms.Support
{
    public struct Rectangle(Vector2 position, Vector2 size)
    {
        public Vector2 Position { get; set; } = position;
        public Vector2 Size { get; set; } = size;

        public float X => Position.X;
        public float Y => Position.Y;

        public float Width => Size.X;
        public float Height => Size.Y;
    }
}
