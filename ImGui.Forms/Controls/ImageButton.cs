using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class ImageButton : Component
    {
        public ImageResource Image { get; set; }
        public Vector2 ImageSize { get; set; } = Vector2.Zero;

        public Vector2 Padding { get; set; } = new Vector2(2, 2);

        #region Events

        public event EventHandler Clicked;

        #endregion

        public override Size GetSize()
        {
            var size = GetImageSize();
            return new Size((int)size.X + (int)Padding.X * 2, (int)size.Y + (int)Padding.Y * 2);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var enabled = Enabled;
            ApplyStyles(enabled);

            if ((IntPtr)Image != IntPtr.Zero)
            {
                if (ImGuiNET.ImGui.ImageButton((IntPtr)Image, GetImageSize()) && Enabled)
                    OnClicked();
            }
            else
            {
                if (ImGuiNET.ImGui.Button(string.Empty, GetImageSize() + Padding * 2) && Enabled)
                    OnClicked();
            }

            RemoveStyles(enabled);
        }

        private void ApplyStyles(bool enabled)
        {
            if (!enabled)
            {
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, 0xFF666666);
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF666666);
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF666666);
            }

            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Padding);
        }

        private void RemoveStyles(bool enabled)
        {
            ImGuiNET.ImGui.PopStyleVar();

            if (!enabled)
                ImGuiNET.ImGui.PopStyleColor(3);
        }

        private Vector2 GetImageSize()
        {
            return ImageSize != Vector2.Zero ? ImageSize : Image?.Size ?? Vector2.Zero;
        }

        private void OnClicked()
        {
            Clicked?.Invoke(this, new EventArgs());
        }
    }
}
