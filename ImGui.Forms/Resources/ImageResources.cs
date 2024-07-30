using System;

namespace ImGui.Forms.Resources
{
    /// <summary>
    /// Allows access to built-in images.
    /// </summary>
    /// <remarks>To load and use your own images, see <see cref="ImageResource.FromFile"/>, <see cref="ImageResource.FromResource"/>, <see cref="ImageResource.FromStream"/>, and <see cref="ImageResource.FromImage"/>.</remarks>
    public static class ImageResources
    {
        private const string ErrorPath_ = "error.png";

        private static readonly Lazy<ThemedImageResource> ErrorLazy = new(() => ImageResource.FromResource(typeof(ImageResources).Assembly, ErrorPath_));

        /// <summary>
        /// Creates a new <see cref="ThemedImageResource"/> for an "Error" symbol.
        /// </summary>
        /// <returns>An <see cref="ThemedImageResource"/> representing an "Error" symbol.</returns>
        public static ThemedImageResource Error => ErrorLazy.Value;
    }
}
