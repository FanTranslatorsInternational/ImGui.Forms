using ImGui.Forms.Models;
using ImGuiNET;
using System;
using System.Numerics;
using Veldrid;

namespace ImGui.Forms.Controls.Base;

public class ZoomableComponent : Component
{
    private Matrix3x2 _transform = new(1, 0, 0, 1, 0, 0);

    private bool _mouseDown;
    private Vector2 _mouseDownPosition;

    #region Properties

    public Size Size { get; set; } = Size.Parent;

    #endregion

    #region Events

    public event EventHandler ContentZoomed;

    public event EventHandler ContentMoved;

    #endregion

    public override Size GetSize() => Size;

    public void Zoom(float scale)
    {
        var scaleVector = Vector2.One + new Vector2(scale);
        _transform *= Matrix3x2.CreateScale(scaleVector, Vector2.Zero);
    }

    public void Reset()
    {
        _transform = new(1, 0, 0, 1, 0, 0);
    }

    public void CopyTransformTo(ZoomableComponent zoomable)
    {
        zoomable.SetTransform(_transform);
    }

    internal void SetTransform(Matrix3x2 matrix)
    {
        _transform = matrix;
    }

    protected override void UpdateInternal(Rectangle contentRect)
    {
        if (ImGuiNET.ImGui.BeginChild($"{Id}", contentRect.Size))
        {
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

                OnContentZoomed();
            }

            // On mouse down, re-translate matrix
            if (!_mouseDown && !ImGuiNET.ImGui.IsMouseDragging(ImGuiMouseButton.Right) &&
                ImGuiNET.ImGui.IsItemHovered() && ImGuiNET.ImGui.IsMouseDown(ImGuiMouseButton.Right))
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

                OnContentMoved();
            }

            DrawInternal(contentRect);
        }

        ImGuiNET.ImGui.EndChild();
    }

    protected virtual void DrawInternal(Rectangle contentRect) { }

    protected Rectangle Transform(Rectangle contentRect, Rectangle toTransform)
    {
        var contentCenterPosition = contentRect.Position + contentRect.Size / 2;

        var scaledContentSize = new Vector2(toTransform.Width * _transform.M11, toTransform.Height * _transform.M22);
        var scaledContentPosition = new Vector2(toTransform.X * _transform.M11, toTransform.Y * _transform.M22);

        var absoluteContentPosition = contentCenterPosition + _transform.Translation + scaledContentPosition;
        var absoluteContentEndPosition = absoluteContentPosition + scaledContentSize;

        return new Rectangle(
            (int)absoluteContentPosition.X,
            (int)absoluteContentPosition.Y,
            (int)(absoluteContentEndPosition.X - absoluteContentPosition.X),
            (int)(absoluteContentEndPosition.Y - absoluteContentPosition.Y)
        );
    }

    protected Vector2 Transform(Rectangle contentRect, Vector2 toTransform)
    {
        var contentCenterPosition = contentRect.Position + contentRect.Size / 2;

        var scaledContentPosition = new Vector2(toTransform.X * _transform.M11, toTransform.Y * _transform.M22);

        var absoluteContentPosition = contentCenterPosition + _transform.Translation + scaledContentPosition;

        return absoluteContentPosition;
    }

    protected Vector2 UnTransform(Rectangle contentRect, Vector2 toTransform)
    {
        var contentCenterPosition = contentRect.Position + contentRect.Size / 2;

        var scaledContentPosition = toTransform - contentCenterPosition - _transform.Translation;

        var untransformedPosition = new Vector2(scaledContentPosition.X / _transform.M11, scaledContentPosition.Y / _transform.M22);

        return untransformedPosition;
    }

    private void OnContentZoomed()
    {
        ContentZoomed?.Invoke(this, EventArgs.Empty);
    }

    private void OnContentMoved()
    {
        ContentMoved?.Invoke(this, EventArgs.Empty);
    }
}