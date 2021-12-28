namespace ImGui.Forms.Support.Veldrid.ImGui
{
    /// <summary>
    /// Identifies the kind of color space handling that an <see cref="ImGuiRenderer"/> uses.
    /// </summary>
    /// <remarks>Implemented directly to the project, so a hard dependency on Veldrid.ImGui's ImGui.NET can be avoided. ImGui.Forms should have the dependency privilege.</remarks>
    public enum ColorSpaceHandling
    {
        /// <summary>
        /// Legacy-style color space handling. In this mode, the renderer will not convert sRGB vertex colors into linear space
        /// before blending them.
        /// </summary>
        Legacy = 0,
        /// <summary>
        /// Improved color space handling. In this mode, the render will convert sRGB vertex colors into linear space before
        /// blending them with colors from user Textures.
        /// </summary>
        Linear = 1,
    }
}
