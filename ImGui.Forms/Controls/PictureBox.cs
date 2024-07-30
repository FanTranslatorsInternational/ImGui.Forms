using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class PictureBox : Component
    {
        private ThemedImageResource _baseImg;

        public ThemedImageResource Image
        {
            get => _baseImg;
            set
            {
                _baseImg?.Destroy();
                _baseImg = value;
            }
        }

        public override Size GetSize()
        {
            return new Size(_baseImg?.Width ?? 0, _baseImg?.Height ?? 0);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            if (_baseImg == null || (nint)_baseImg == nint.Zero)
                return;

            ImGuiNET.ImGui.Image((nint)Image, new Vector2(_baseImg.Width, _baseImg.Height));
        }
    }
}
