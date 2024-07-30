using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.IO;
using System.Numerics;
using System.Reflection;

namespace ImGui.Forms.Resources
{
    /// <summary>
    /// Represents a single image in ImGui.Forms.
    /// </summary>
    /// <remarks>To load built-in images, see <see cref="ImageResource"/>.</remarks>
    public class ImageResource
    {
        private readonly Image<Rgba32> _img;
        private nint _ptr;

        /// <summary>
        /// The size of the <see cref="ImageResource"/> as a <see cref="Vector2"/>.
        /// </summary>
        public Vector2 Size => new(_img.Width, _img.Height);

        /// <summary>
        /// The width of the <see cref="ImageResource"/>.
        /// </summary>
        public int Width => _img.Width;

        /// <summary>
        /// The height of the <see cref="ImageResource"/>.
        /// </summary>
        public int Height => _img.Height;

        /// <summary>
        /// Creates a new <see cref="ImageResource"/>.
        /// </summary>
        /// <param name="image">The image to load in this <see cref="ImageResource"/>.</param>
        private ImageResource(Image<Rgba32> image)
        {
            _img = image;
        }

        #region Load Methods

        /// <summary>
        /// Creates a new <see cref="ImageResource"/> from <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to load the image from.</param>
        /// <returns>An <see cref="ImageResource"/> representing the image from the given <paramref name="path"/>.</returns>
        /// <remarks>To load built-in images, see <see cref="ImageResources"/>.</remarks>
        public static ImageResource FromFile(string path)
        {
            return FromImage(Image.Load<Rgba32>(path));
        }

        /// <summary>
        /// Creates a new <see cref="ImageResource"/> from an embedded resource <paramref name="resourceName"/> in <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to load the embedded resource from.</param>
        /// <param name="resourceName">The name of the resource to load.</param>
        /// <returns>An <see cref="ImageResource"/> representing the image.</returns>
        /// <remarks>To load built-in images, see <see cref="ImageResources"/>.</remarks>
        public static ImageResource FromResource(Assembly assembly, string resourceName)
        {
            return FromStream(assembly.GetManifestResourceStream(resourceName));
        }

        /// <summary>
        /// Creates a new <see cref="ImageResource"/> from <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to load the image from.</param>
        /// <returns>An <see cref="ImageResource"/> representing the image from the given <paramref name="stream"/>.</returns>
        /// <remarks>To load built-in images, see <see cref="ImageResources"/>.</remarks>
        public static ImageResource FromStream(Stream stream)
        {
            return FromImage(Image.Load<Rgba32>(stream));
        }

        /// <summary>
        /// Creates a new <see cref="ImageResource"/> from <paramref name="image"/>.
        /// </summary>
        /// <param name="image">The <see cref="Image{TPixel}"/> to load.</param>
        /// <returns>An <see cref="ImageResource"/> representing <paramref name="image"/>.</returns>
        /// <remarks>To load built-in images, see <see cref="ImageResources"/>.</remarks>
        public static ImageResource FromImage(Image<Rgba32> image)
        {
            return new ImageResource(image);
        }

        #endregion

        public void Destroy()
        {
            if (_ptr != nint.Zero)
                Application.Instance?.ImageFactory.UnloadImage(_ptr);

            _ptr = nint.Zero;
        }

        public static explicit operator nint(ImageResource ir) => ir.GetPointer();

        private nint GetPointer()
        {
            if (_ptr != nint.Zero)
                return _ptr;

            return _ptr = Application.Instance?.ImageFactory.LoadImage(_img) ?? nint.Zero;
        }
    }
}
