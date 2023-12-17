using System;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGui.Forms.Support;
using ImGuiNET;

namespace ImGui.Forms.Controls
{
    public class ZoomablePictureBox : Component
    {
        private Matrix3x2 _transform = new Matrix3x2(1, 0, 0, 1, 0, 0);

        private bool _mouseDown;
        private Vector2 _mouseDownPosition;

        private ImageResource _baseImg;

        public ImageResource Image
        {
            get => _baseImg;
            set
            {
                _baseImg?.Destroy();
                _baseImg = value;
            }
        }

        #region Events

        public event EventHandler MouseScrolled;

        #endregion

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override void UpdateInternal(Veldrid.Rectangle contentRect)
        {
            if (Image == null || (IntPtr)_baseImg == IntPtr.Zero)
                return;

            ImGuiSupport.Dummy(Id, contentRect.Position, contentRect.Size);

            var componentCenterPosition = new Vector2(contentRect.X, contentRect.Y) + new Vector2((float)contentRect.Width / 2, (float)contentRect.Height / 2);
            var translatedComponentCenterPosition = componentCenterPosition + _transform.Translation;

            var io = ImGuiNET.ImGui.GetIO();

            // On mouse scroll, rescale matrix
            if (io.MouseWheel != 0 && IsHovering(contentRect))
            {
                var scale = Vector2.One + new Vector2(io.MouseWheel / 8);
                var translatedMousePosition = io.MousePos + _transform.Translation;
                _transform *= Matrix3x2.CreateScale(scale, translatedMousePosition - translatedComponentCenterPosition);

                OnMouseScrolled();
            }

            // On mouse down, re-translate matrix
            if (!_mouseDown && IsHovering(contentRect) && ImGuiNET.ImGui.IsMouseDown(ImGuiMouseButton.Right))
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

            if (_mouseDown)
            {
                _transform *= Matrix3x2.CreateTranslation(ImGuiNET.ImGui.GetMousePos() - _mouseDownPosition);
                _mouseDownPosition = ImGuiNET.ImGui.GetMousePos();
            }

            var scaledContentSize = new Vector2(_baseImg.Width * _transform.M11, _baseImg.Height * _transform.M22);
            var scaledContentCenterPosition = scaledContentSize / new Vector2(2, 2);

            var absoluteContentPosition = componentCenterPosition + _transform.Translation - scaledContentCenterPosition;
            var absoluteContentEndPosition = absoluteContentPosition + scaledContentSize;

            ImGuiNET.ImGui.GetWindowDrawList().AddImage((IntPtr)_baseImg, absoluteContentPosition, absoluteContentEndPosition);
        }

        private bool IsHovering(Veldrid.Rectangle contentRect)
        {
            return ImGuiNET.ImGui.IsItemHovered();
            //ImGuiNET.ImGui.IsMouseHoveringRect(new Vector2(contentRect.X, contentRect.Y),
            //    new Vector2(contentRect.X + contentRect.Width, contentRect.Y + contentRect.Height));
        }

        private void OnMouseScrolled()
        {
            MouseScrolled?.Invoke(this, EventArgs.Empty);
        }
    }
}
