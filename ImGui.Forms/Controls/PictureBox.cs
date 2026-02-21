using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGui.Forms.Support;

namespace ImGui.Forms.Controls;

public class PictureBox : Component
{
    #region Properties

    public Size Size { get; set; } = Size.Content;

    public ThemedImageResource? Image { get; private set; }

    #endregion

    public PictureBox(ThemedImageResource? image = null)
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
        if (Image == null || !Image.IsValid())
            return;

        Hexa.NET.ImGui.ImGui.Image(Image.GetTextureRef(), contentRect.Size);
    }
}