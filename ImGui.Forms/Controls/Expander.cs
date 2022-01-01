using System;
using System.Collections.Generic;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class Expander : Component
    {
        private const int CircleRadius_ = 10;
        private const int CircleDiameter_ = CircleRadius_ * 2;

        public string Caption { get; set; } = string.Empty;

        public Component Content { get; set; }

        public int ContentHeight { get; set; } = 200;

        public bool Expanded { get; set; }

        #region Events

        public event EventHandler ExpandedChanged;

        #endregion

        public override Size GetSize()
        {
            var size = ImGuiNET.ImGui.CalcTextSize(Caption);
            return new Size(1f, (int)(Math.Max((int)Math.Ceiling(size.Y), CircleDiameter_) + (Expanded ? ContentHeight : 0)));
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var size = ImGuiNET.ImGui.CalcTextSize(Caption);
            var headerHeight = Math.Max((int)Math.Ceiling(size.Y), CircleDiameter_);

            // Draw expansion icon
            var pos = contentRect.Position + new Vector2(0, (headerHeight - CircleDiameter_) / 2f);

            var arrowArea = new Rectangle(contentRect.X, contentRect.Y, CircleDiameter_, CircleDiameter_);
            var isHovering = IsHovering(arrowArea);
            var isMouseDown = isHovering && ImGuiNET.ImGui.IsMouseDown(ImGuiMouseButton.Left);

            var circleColor = isMouseDown ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonActive) : isHovering ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered) : ImGuiNET.ImGui.GetColorU32(ImGuiCol.Button);
            var triangleColor = isMouseDown ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonActive) : isHovering ? ImGuiNET.ImGui.GetColorU32(ImGuiCol.ButtonHovered) : ImGuiNET.ImGui.GetColorU32(ImGuiCol.Button);

            var triPoints = GetTriangle();
            ImGuiNET.ImGui.GetWindowDrawList().AddCircle(pos + new Vector2(CircleRadius_, (headerHeight - CircleDiameter_) / 2 + CircleRadius_), CircleRadius_, circleColor, -1, 1.5f);
            ImGuiNET.ImGui.GetWindowDrawList().AddTriangleFilled(pos + triPoints[0], pos + triPoints[1], pos + triPoints[2], triangleColor);

            // Draw text
            pos = contentRect.Position + new Vector2(CircleDiameter_ + ImGuiNET.ImGui.GetStyle().ItemSpacing.X, (headerHeight - size.Y) / 2);
            ImGuiNET.ImGui.GetWindowDrawList().AddText(pos, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text), Caption ?? string.Empty);

            // Draw content
            if (Expanded)
            {
                ImGuiNET.ImGui.SetCursorPos(new Vector2(0, headerHeight));
                Content?.Update(new Rectangle(contentRect.X, contentRect.Y + headerHeight, contentRect.Width, ContentHeight));
            }

            // Check if item should be expanded
            if (isHovering && ImGuiNET.ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                Expanded = !Expanded;
                OnExpandedChanged();
            }
        }

        private bool IsHovering(Rectangle contentRect)
        {
            return ImGuiNET.ImGui.IsMouseHoveringRect(contentRect.Position, contentRect.Position + contentRect.Size);
        }

        private IList<Vector2> GetTriangle()
        {
            var heightOffset = Expanded ? 3 : -3;
            return new List<Vector2>
            {
                new Vector2(CircleRadius_ - 3, CircleRadius_ + heightOffset),
                new Vector2(CircleRadius_ + 3, CircleRadius_ + heightOffset),
                new Vector2(CircleRadius_, CircleRadius_ - heightOffset)
            };
        }

        private void OnExpandedChanged()
        {
            ExpandedChanged?.Invoke(this, new EventArgs());
        }
    }
}
