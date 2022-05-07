namespace ImGui.Forms.Resources
{
    /// <summary>
    /// Allows access to built-in fonts.
    /// </summary>
    /// <remarks>To load and use your own fonts, see <see cref="FontResource.FromFile"/> and <see cref="FontResource.FromResource"/>.</remarks>
    public static class FontResources
    {
        private const string ArialPath_ = "arial.ttf";
        private const string RobotoPath_ = "roboto.ttf";

        /// <summary>
        /// Registers the font "Arial" with <paramref name="size"/> into the application. Has to be called before the application was started.
        /// </summary>
        /// <param name="size">The size to display the font in.</param>
        public static void RegisterArial(int size)
        {
            Application.FontFactory.RegisterFromResource(typeof(FontResources).Assembly, ArialPath_, size);
        }

        /// <summary>
        /// Registers the font "Roboto" with <paramref name="size"/> into the application. Has to be called before the application was started.
        /// </summary>
        /// <param name="size">The size to display the font in.</param>
        public static void RegisterRoboto(int size)
        {
            Application.FontFactory.RegisterFromResource(typeof(FontResources).Assembly, RobotoPath_, size);
        }

        /// <summary>
        /// Gets a <see cref="FontResource"/> representing "Arial" with <paramref name="size"/>. Has to be registered with <see cref="RegisterArial(int)"/> before the application was started.
        /// </summary>
        /// <param name="size">The size to display the font in.</param>
        /// <returns>A <see cref="FontResource"/> representing "Arial" in the given <paramref name="size"/>.</returns>
        public static FontResource Arial(int size)
        {
            return Application.FontFactory.Get(typeof(FontResources).Assembly, ArialPath_, size);
        }

        /// <summary>
        /// Gets a <see cref="FontResource"/> representing "Roboto" with <paramref name="size"/>. Has to be registered with <see cref="RegisterArial(int)"/> before the application was started.
        /// </summary>
        /// <param name="size"></param>
        /// <returns>A <see cref="FontResource"/> representing "Roboto" in the given <paramref name="size"/>.</returns>
        public static FontResource Roboto(int size)
        {
            return Application.FontFactory.Get(typeof(FontResources).Assembly, RobotoPath_, size);
        }
    }
}
