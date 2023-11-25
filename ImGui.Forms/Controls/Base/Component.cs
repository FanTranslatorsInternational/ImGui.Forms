using System;
using System.Drawing;
using ImGui.Forms.Extensions;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGuiNET;
using Veldrid.Sdl2;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls.Base
{
    // TODO: Due to executive inconsistency, remove ApplyStyles/RemoveStyles methods and apply styles in Update/Size methods directly
    public abstract class Component
    {
        /// <summary>
        /// The Id for this component.
        /// </summary>
        public int Id => Application.Instance.IdFactory.Get(this);

        /// <summary>
        /// Declares if a component is visible and should be drawn with its content.
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Declares if a component should react to external events.
        /// </summary>
        /// <remarks>Components that implement their own events should refer to this property to determine if they should be invoked.</remarks>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Allows drag and drop support for this component.
        /// </summary>
        public bool AllowDragDrop { get; set; }

        /// <summary>
        /// Determines if the component should have a visible border.
        /// </summary>
        public bool ShowBorder { get; set; }

        #region Events

        /// <summary>
        /// The event to intercept DragDrop actions.
        /// </summary>
        public event EventHandler<DragDropEvent> DragDrop;

        #endregion

        /// <summary>
        /// Updates the components content to ImGui.
        /// </summary>
        public void Update(Rectangle contentRect)
        {
            // Handle visibility
            if (!Visible)
                return;

            // Handle drawing of component
            ImGuiNET.ImGui.PushID(Id);

            // Draw border
            if (ShowBorder)
                ImGuiNET.ImGui.GetWindowDrawList().AddRect(contentRect.Position, contentRect.Position + contentRect.Size, Style.GetColor(ImGuiCol.Border).ToUInt32(), 0);

            ApplyStyles();
            UpdateInternal(contentRect);
            RemoveStyles();

            ImGuiNET.ImGui.PopID();

            // Handle Drag and Drop after rendering, so drag drop events go from most nested to least nested control
            // HINT: Don't handle Drag and Drop if the component either doesn't allow it or the component is marked as disabled.
            if (!AllowDragDrop || !Enabled)
                return;

            if (Application.Instance.TryGetDragDrop(contentRect, out var dragDrop))
                OnDragDrop(dragDrop.Event);
        }

        /// <summary>
        /// Determines if the current ImGui item is hovered.
        /// </summary>
        /// <returns>If the current ImGui item is hovered.</returns>
        protected bool IsHoveredCore()
        {
            return ImGuiNET.ImGui.IsItemHovered();
        }

        /// <summary>
        /// Determines if the current ImGui item is active.
        /// </summary>
        /// <returns>If the current ImGui item is active.</returns>
        protected bool IsActiveCore()
        {
            return ImGuiNET.ImGui.IsItemActive() && ImGuiNET.ImGui.IsItemHovered();
        }

        /// <summary>
        /// Gets the finalized width of this component.
        /// </summary>
        /// <param name="parentWidth">The width of the parent component.</param>
        /// <param name="layoutCorrection">A correctional value for layouts.</param>
        /// <returns>The finalized width of this component.</returns>
        public int GetWidth(int parentWidth, float layoutCorrection = 1f)
        {
            var width = GetSize().Width;
            return width.IsContentAligned ?
                GetContentWidth(parentWidth, layoutCorrection) :
                GetDimension(width, parentWidth, layoutCorrection);
        }

        /// <summary>
        /// Gets the width of this component, if it's content aligned.
        /// </summary>
        /// <param name="parentWidth">The width of the parent component.</param>
        /// <param name="layoutCorrection">A correctional value for layouts.</param>
        /// <returns>The content width of this component.</returns>
        protected virtual int GetContentWidth(int parentWidth, float layoutCorrection = 1f) => 0;

        /// <summary>
        /// Gets the finalized height of this component.
        /// </summary>
        /// <param name="parentHeight">The height of the parent component.</param>
        /// <param name="layoutCorrection">A correctional value for layouts.</param>
        /// <returns>The finalized height of this component.</returns>
        public int GetHeight(int parentHeight, float layoutCorrection = 1f)
        {
            var height = GetSize().Height;
            return height.IsContentAligned ?
                GetContentHeight(parentHeight, layoutCorrection) :
                GetDimension(height, parentHeight, layoutCorrection);
        }

        /// <summary>
        /// Gets the height of this component, if it's content aligned.
        /// </summary>
        /// <param name="parentHeight">The height of the parent component.</param>
        /// <param name="layoutCorrection">A correctional value for layouts.</param>
        /// <returns>The content height of this component.</returns>
        protected virtual int GetContentHeight(int parentHeight, float layoutCorrection = 1f) => 0;

        /// <summary>
        /// Gets the absolute or relative size of the component. Use <see cref="GetWidth"/> or <see cref="GetHeight"/> to get the finalized size.
        /// </summary>
        /// <returns>The absolute or relative size of this component.</returns>
        /// TODO: Make gettable property, default value Size.Content
        public abstract Size GetSize();

        /// <summary>
        /// Updates the component.
        /// </summary>
        /// <param name="contentRect">The rectangle in which to draw the component.</param>
        protected abstract void UpdateInternal(Rectangle contentRect);

        /// <summary>
        /// Applies any styles specific to this component, before <see cref="UpdateInternal"/> is invoked.
        /// </summary>
        protected virtual void ApplyStyles() { }

        /// <summary>
        /// Removes any styles specific to this component, after <see cref="UpdateInternal"/> is invoked.
        /// </summary>
        protected virtual void RemoveStyles() { }

        protected bool IsKeyDown(KeyCommand keyDown)
        {
            if (keyDown == default)
                return false;

            if (!Application.Instance.TryGetKeyDownCommand(out KeyCommand internalKeyDown))
                return false;

            return keyDown == internalKeyDown;
        }

        protected bool IsKeyUp(KeyCommand keyUp)
        {
            if (keyUp == default)
                return false;

            if (!Application.Instance.TryGetKeyDownCommand(out KeyCommand internalKeyUp))
                return false;

            return keyUp == internalKeyUp;
        }

        /// <summary>
        /// Invoke the DragDrop event of this component.
        /// </summary>
        /// <param name="obj">The drag drop object received.</param>
        private void OnDragDrop(DragDropEvent obj)
        {
            DragDrop?.Invoke(this, obj);
        }

        /// <summary>
        /// Calculates the integer value of a <see cref="SizeValue"/>.
        /// </summary>
        /// <param name="dimensionValue">The value to calculate.</param>
        /// <param name="maxDimensionValue">The maximum to calculate relative <see cref="SizeValue"/>s against.</param>
        /// <param name="correction">The corrective value to calculate relative <see cref="SizeValue"/>s against.</param>
        /// <returns></returns>
        internal static int GetDimension(SizeValue dimensionValue, int maxDimensionValue, float correction = 1f)
        {
            if (dimensionValue.IsAbsolute)
                return (int)Math.Min(dimensionValue.Value, maxDimensionValue);

            return (int)Math.Floor(dimensionValue.Value * maxDimensionValue * correction);
        }
    }
}
