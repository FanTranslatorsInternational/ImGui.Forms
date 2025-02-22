﻿using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;

namespace ImGui.Forms.Controls
{
    public class ZoomablePictureBox : Component
    {
        private Matrix3x2 _transform = new(1, 0, 0, 1, 0, 0);

        private bool _mouseDown;
        private Vector2 _mouseDownPosition;

        private ThemedImageResource _baseImg;

        #region Properties

        public ThemedImageResource Image
        {
            get => _baseImg;
            set
            {
                _baseImg?.Destroy();
                _baseImg = value;
            }
        }

        public Size Size { get; set; } = Size.Parent;

        public bool ShowImageBorder { get; set; }

        #endregion

        #region Events

        public event EventHandler MouseScrolled;

        #endregion

        public ZoomablePictureBox(ThemedImageResource image = default)
        {
            Image = image;
        }

        public override Size GetSize() => Size;

        public void Zoom(float scale)
        {
            var scaleVector = Vector2.One + new Vector2(scale);
            _transform *= Matrix3x2.CreateScale(scaleVector, Vector2.Zero);
        }

        protected override void UpdateInternal(Veldrid.Rectangle contentRect)
        {
            if (Image == null || (nint)_baseImg == nint.Zero)
                return;

            ImGuiNET.ImGui.Dummy(contentRect.Size);

            var componentCenterPosition = contentRect.Position + contentRect.Size / 2;
            var translatedComponentCenterPosition = componentCenterPosition + _transform.Translation;

            var io = ImGuiNET.ImGui.GetIO();

            // On mouse scroll, rescale matrix
            if (io.MouseWheel != 0 && ImGuiNET.ImGui.IsItemHovered())
            {
                var scale = Vector2.One + new Vector2(io.MouseWheel / 8);
                var translatedMousePosition = io.MousePos + _transform.Translation;
                _transform *= Matrix3x2.CreateScale(scale, translatedMousePosition - translatedComponentCenterPosition);

                OnMouseScrolled();
            }

            // On mouse down, re-translate matrix
            if (!_mouseDown && ImGuiNET.ImGui.IsItemHovered() && ImGuiNET.ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                _mouseDownPosition = ImGuiNET.ImGui.GetMousePos();
                _mouseDown = true;

                ImGuiNET.ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNESW);
            }

            if (ImGuiNET.ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                _mouseDownPosition = Vector2.Zero;
                _mouseDown = false;

                ImGuiNET.ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
            }

            if (_mouseDown && ImGuiNET.ImGui.IsItemHovered())
            {
                _transform *= Matrix3x2.CreateTranslation(ImGuiNET.ImGui.GetMousePos() - _mouseDownPosition);
                _mouseDownPosition = ImGuiNET.ImGui.GetMousePos();
            }

            var scaledContentSize = new Vector2(_baseImg.Width * _transform.M11, _baseImg.Height * _transform.M22);
            var scaledContentCenterPosition = scaledContentSize / new Vector2(2, 2);

            var absoluteContentPosition = componentCenterPosition + _transform.Translation - scaledContentCenterPosition;
            var absoluteContentEndPosition = absoluteContentPosition + scaledContentSize;

            ImGuiNET.ImGui.GetWindowDrawList().AddImage((nint)_baseImg, absoluteContentPosition, absoluteContentEndPosition);
            if (ShowImageBorder)
                ImGuiNET.ImGui.GetWindowDrawList().AddRect(absoluteContentPosition, absoluteContentEndPosition, Style.GetColor(ImGuiCol.Border).ToUInt32());
        }

        private void OnMouseScrolled()
        {
            MouseScrolled?.Invoke(this, EventArgs.Empty);
        }
    }
}
