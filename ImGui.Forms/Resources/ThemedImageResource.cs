using System;
using System.Numerics;
using Hexa.NET.ImGui;
using ImGui.Forms.Models;

namespace ImGui.Forms.Resources;

public class ThemedImageResource(ImageResource? lightImage, ImageResource? darkImage)
{
    /// <summary>
    /// The size of the <see cref="ThemedImageResource"/> as a <see cref="Vector2"/>.
    /// </summary>
    public Vector2 Size => new(Width, Height);

    /// <summary>
    /// The width of the <see cref="ThemedImageResource"/>.
    /// </summary>
    public int Width => GetImage()?.Width ?? 0;

    /// <summary>
    /// The height of the <see cref="ThemedImageResource"/>.
    /// </summary>
    public int Height => GetImage()?.Height ?? 0;

    public void Destroy()
    {
        lightImage?.Destroy();
        darkImage?.Destroy();
    }

    public bool IsValid()
    {
        var textureRef = GetTextureRef();
        
        if (textureRef is null)
            return false;

        return textureRef.Value.TexID != nint.Zero;
    }

    public ImTextureRef? GetTextureRef()
    {
        return GetImage()?.GetTextureRef();
    }

    public static implicit operator ThemedImageResource(ImageResource? ir) => new(ir, ir);

    public ImageResource? GetImage()
    {
        return Style.Theme switch
        {
            Theme.Light => lightImage,
            Theme.Dark => darkImage,
            _ => throw new InvalidOperationException($"Unknown theme {Style.Theme}.")
        };
    }
}