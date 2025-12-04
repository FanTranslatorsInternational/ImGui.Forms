using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

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
            ImGuiNET.ImGui.GetWindowDrawList().AddRectFilled(contentRect.Position, contentRect.Position + contentRect.Size, BackgroundColor.ToUInt32());

        // Draw image
        if (!HasValidImage())
            return;

        Rectangle imageRect = GetTransformedImageRect(contentRect);

        ImGuiNET.ImGui.GetWindowDrawList().AddImage((nint)Image!, imageRect.Position, imageRect.Position + imageRect.Size);

        if (ShowImageBorder)
            ImGuiNET.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Style.GetColor(ImGuiCol.Border).ToUInt32());
    }

    protected bool HasValidImage()
    {
        return Image != null && (nint)Image != nint.Zero;
    }

    protected Rectangle GetTransformedImageRect(Rectangle contentRect)
    {
        var imageStartPosition = -(Image!.Size / 2);
        var imageRect = new Rectangle((int)imageStartPosition.X, (int)imageStartPosition.Y, Image.Width, Image.Height);
        return Transform(contentRect, imageRect);
    }
}