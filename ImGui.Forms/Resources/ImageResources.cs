using System;

namespace ImGui.Forms.Resources
{
    /// <summary>
    /// Allows access to built-in images.
    /// </summary>
    /// <remarks>To load and use your own images, see <see cref="ImageResource.FromFile"/>, <see cref="ImageResource.FromResource"/>, <see cref="ImageResource.FromStream"/>, and <see cref="ImageResource.FromBitmap"/>.</remarks>
    public static class ImageResources
    {
        private const string ErrorPath_ = "error.png";

        private static readonly Lazy<ImageResource> ErrorLazy = new Lazy<ImageResource>(() => ImageResource.FromResource(typeof(ImageResources).Assembly, ErrorPath_));

        /// <summary>
        /// Creates a new <see cref="ImageResource"/> for an "Error" symbol.
        /// </summary>
        /// <returns>An <see cref="ImageResource"/> representing an "Error" symbol.</returns>
        public static ImageResource Error() => ErrorLazy.Value;
    }
}
