using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class ImageButton : Component
    {
        private ThemedImageResource _baseImg;

        #region Properties

        public LocalizedString? Tooltip { get; set; }

        public KeyCommand KeyAction { get; set; }

        public ThemedImageResource Image
        {
            get => _baseImg;
            set
            {
                _baseImg?.Destroy();
                _baseImg = value;
            }
        }

        public Vector2 ImageSize { get; set; } = Vector2.Zero;

        public Vector2 Padding { get; set; } = new(2, 2);

        #endregion

        #region Events

        public event EventHandler Clicked;

        #endregion

        public ImageButton(ThemedImageResource image = default)
        {
            Image = image;
        }

        public override Size GetSize()
        {
            var size = GetImageSize();
            return new Size((int)size.X + (int)Padding.X * 2, (int)size.Y + (int)Padding.Y * 2);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var enabled = Enabled;
            ApplyStyles(enabled);

            if ((nint)Image != nint.Zero)
            {
                if ((ImGuiNET.ImGui.ImageButton($"##{Id}", (nint)Image, GetImageSize()) || IsKeyDown(KeyAction)) && Enabled)
                    OnClicked();
            }
            else
            {
                if ((ImGuiNET.ImGui.Button(string.Empty, GetImageSize() + Padding * 2) || IsKeyDown(KeyAction)) && Enabled)
                    OnClicked();
            }

            if (Enabled && Tooltip is { IsEmpty: false } && IsHoveredCore())
            {
                ImGuiNET.ImGui.BeginTooltip();
                ImGuiNET.ImGui.Text(Tooltip);
                ImGuiNET.ImGui.EndTooltip();
            }

            RemoveStyles(enabled);
        }

        private void ApplyStyles(bool enabled)
        {
            if (!enabled)
            {
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
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
