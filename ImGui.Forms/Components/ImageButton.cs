using System;
using System.Numerics;
using ImGui.Forms.Components.Base;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Components
{
    public class ImageButton : Component
    {
        public ImageResource Image { get; set; }

        public bool Enabled { get; set; } = true;

        #region Events

        public event EventHandler Clicked;

        #endregion

        public override Size GetSize()
        {
            return new Size(Image?.Width ?? 0, Image?.Height ?? 0);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            if (Image == null || (IntPtr)Image == IntPtr.Zero)
                return;

            if (ImGuiNET.ImGui.ImageButton((IntPtr)Image, new Vector2(Image.Width, Image.Height)) && Enabled)
                OnClicked();
        }

        protected override void ApplyStyles()
        {
            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0,0));
        }

        protected override void RemoveStyles()
        {
            ImGuiNET.ImGui.PopStyleVar();
        }

        private void OnClicked()
        {
            Clicked?.Invoke(this, new EventArgs());
        }
    }
}
