using System;

namespace ImGui.Forms.Models
{
    public class Size
    {
        /// <summary>
        /// The width of this instance.
        /// </summary>
        public SizeValue Width { get; set; }

        /// <summary>
        /// The height of this instance.
        /// </summary>
        public SizeValue Height { get; set; }

        /// <summary>
        /// An empty size with width and height 0.
        /// </summary>
        public static readonly Size Empty = new Size(0, 0);

        /// <summary>
        /// A size adjusting to fill the parent container.
        /// </summary>
        public static readonly Size Parent = new Size(1f, 1f);

        /// <summary>
        /// A size adjusting to the contents of this component.
        /// </summary>
        public static readonly Size Content = new Size(-1, -1);

        public Size(SizeValue width, SizeValue height)
        {
            Width = width;
            Height = height;
        }
    }

    public class SizeValue
    {
        /// <summary>
        /// Gets the absolute value or the the relative factor between 0.0 and 1.0.
        /// </summary>
        public float Value { get; }

        /// <summary>
        /// Declares if the value is absolute or relative.
        /// </summary>
        public bool IsAbsolute { get; }

        private SizeValue(float value, bool isAbsolute)
        {
            Value = value;
            IsAbsolute = isAbsolute;
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

        public static implicit operator SizeValue(int d) => Absolute(d);
        public static implicit operator SizeValue(float f) => Relative(f);
    }
}
