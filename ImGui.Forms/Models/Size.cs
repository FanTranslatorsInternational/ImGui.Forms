using System;

namespace ImGui.Forms.Models
{
    public readonly struct Size
    {
        /// <summary>
        /// An empty size with width and height 0.
        /// </summary>
        public static readonly Size Empty = new Size(0, 0);

        /// <summary>
        /// A size adjusting to fill the parent container.
        /// </summary>
        public static readonly Size Parent = new Size(SizeValue.Parent, SizeValue.Parent);

        /// <summary>
        /// A size adjusting to the contents of this component.
        /// </summary>
        public static readonly Size Content = new Size(SizeValue.Content, SizeValue.Content);

        /// <summary>
        /// A size where width adjusts to fill the parent container and height adjusts to this component.
        /// </summary>
        public static readonly Size WidthAlign = new Size(SizeValue.Parent, SizeValue.Content);

        /// <summary>
        /// A size where height adjusts to fill the parent container and width adjusts to this component.
        /// </summary>
        public static readonly Size HeightAlign = new Size(SizeValue.Content, SizeValue.Parent);

        /// <summary>
        /// The width of this instance.
        /// </summary>
        public readonly SizeValue Width;

        /// <summary>
        /// The height of this instance.
        /// </summary>
        public readonly SizeValue Height;

        /// <summary>
        /// Determines if the given size should conform to the content.
        /// </summary>
        public bool IsContentAligned => Width.IsContentAligned && Height.IsContentAligned;

        /// <summary>
        /// Determines if the given size should conform to the parent.
        /// </summary>
        public bool IsParentAligned => Width.IsParentAligned && Height.IsParentAligned;

        /// <summary>
        /// Determines if the given size creates a visible area (width or height > 0).
        /// </summary>
        public bool IsVisible => Width.IsVisible && Height.IsVisible;

        public Size(SizeValue width, SizeValue height)
        {
            Width = width;
            Height = height;
        }

        public bool Equals(Size other)
        {
            return Width.Equals(other.Width) && Height.Equals(other.Height);
        }

        public override bool Equals(object obj)
        {
            return obj is Size other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height);
        }

        public static bool operator !=(Size s1, Size s2) => !s1.Equals(s2);
        public static bool operator ==(Size s1, Size s2) => s1.Equals(s2);
    }

    public readonly struct SizeValue
    {
        public static SizeValue Content = Absolute(-1);
        public static SizeValue Parent = Relative(1f);

        /// <summary>
        /// Gets the absolute value or the the relative factor between 0.0 and 1.0.
        /// </summary>
        public readonly float Value;

        /// <summary>
        /// Declares if the value is relative.
        /// </summary>
        public readonly bool IsRelative;

        /// <summary>
        /// Declares if the value is absolute.
        /// </summary>
        public bool IsAbsolute => !IsRelative;

        /// <summary>
        /// Determines if the given size value should conform to the content.
        /// </summary>
        public bool IsContentAligned => IsAbsolute && (int)Value == -1;

        /// <summary>
        /// Determines if the given size value should conform to the parent.
        /// </summary>
        public bool IsParentAligned => IsRelative && (int)Value == 1;

        /// <summary>
        /// Determines if the given size value creates a visible dimension (> 0).
        /// </summary>
        public bool IsVisible => Value != 0f;

        private SizeValue(float value, bool isAbsolute)
        {
            Value = value;
            IsRelative = !isAbsolute;
        }

        /// <summary>
        /// Creates an absolute <see cref="SizeValue"/>.
        /// </summary>
        /// <param name="value">The absolute value.</param>
        /// <returns>The absolute <see cref="SizeValue"/>.</returns>
        public static SizeValue Absolute(int value)
        {
            return new SizeValue(Math.Max(value, -1), true);
        }

        /// <summary>
        /// Creates a relative <see cref="SizeValue"/>.
        /// </summary>
        /// <param name="factor">The relative factor.</param>
        /// <returns>The relative <see cref="SizeValue"/>.</returns>
        public static SizeValue Relative(float factor)
        {
            return new SizeValue(Math.Clamp(factor, 0, 1), false);
        }

        public bool Equals(SizeValue other)
        {
            return Value.Equals(other.Value) && IsAbsolute == other.IsAbsolute;
        }

        public override bool Equals(object obj)
        {
            return obj is SizeValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, IsAbsolute);
        }

        public static bool operator !=(SizeValue s1, SizeValue s2) => !s1.Equals(s2);
        public static bool operator ==(SizeValue s1, SizeValue s2) => s1.Equals(s2);

        public static implicit operator SizeValue(int d) => Absolute(d);
        public static implicit operator SizeValue(float f) => Relative(f);
    }
}
