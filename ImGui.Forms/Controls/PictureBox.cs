using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using Veldrid;

namespace ImGui.Forms.Controls;

public class PictureBox : Component
{
    #region Properties

    public Size Size { get; set; } = Size.Content;

    public ThemedImageResource Image { get; private set; }

    #endregion

    public PictureBox(ThemedImageResource image = default)
    {
        Image = image;
    }

    public void SetImage(ThemedImageResource imagResource, bool releaseOldImage = true)
    {
        if (releaseOldImage)
            Image?.Destroy();

        Image = imagResource;
    }

    public override Size GetSize()
    {
        SizeValue width = Size.Width.IsContentAligned
            ? SizeValue.Absolute(Image?.Width ?? 0)
            : Size.Width;

        SizeValue height = Size.Height.IsContentAligned
            ? SizeValue.Absolute(Image?.Height ?? 0)
            : Size.Height;

        return new Size(width, height);
    }

    protected override void UpdateInternal(Rectangle contentRect)
    {
        if (Image == null || (nint)Image == nint.Zero)
            return;

        ImGuiNET.ImGui.Image((nint)Image, contentRect.Size);
    }
}