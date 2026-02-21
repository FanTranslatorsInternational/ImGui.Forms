using System.Numerics;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Resources;
using ImGui.Forms.Support;

namespace ImGui.Forms.Controls;

public class ZoomablePictureBox : ZoomableComponent
{
    #region Properties

    public ThemedImageResource? Image { get; private set; }

    public bool ShowImageBorder { get; set; }

    public ThemedColor BackgroundColor { get; set; }

    #endregion

    public ZoomablePictureBox(ThemedImageResource? image = null)
    {
        Image = image;
    }

    public void SetImage(ThemedImageResource imagResource, bool releaseOldImage = true)
    {
        if (releaseOldImage)
            Image?.Destroy();

        Image = imagResource;
    }

    protected override void DrawInternal(Rectangle contentRect)
    {
        // Draw background color
        if (!BackgroundColor.IsEmpty)
            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRectFilled(contentRect.Position, contentRect.Position + contentRect.Size, BackgroundColor.ToUInt32());

        // Draw image
        if (!HasValidImage())
            return;

        Rectangle imageRect = GetTransformedImageRect(contentRect);

        Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddImage(Image!.GetTextureRef(), imageRect.Position, imageRect.Position + imageRect.Size);

        if (ShowImageBorder)
            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Style.GetColor(ImGuiCol.Border).ToUInt32());
    }

    protected bool HasValidImage()
    {
        return Image != null && Image.IsValid();
    }

    protected Rectangle GetTransformedImageRect(Rectangle contentRect)
    {
        var imageStartPosition = -(Image!.Size / 2);
        var imageRect = new Rectangle(imageStartPosition, new Vector2(Image.Width, Image.Height));
        return Transform(contentRect, imageRect);
    }
}