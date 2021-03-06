using System;
using ImGui.Forms.Models;
using Veldrid;
using Veldrid.Sdl2;

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
        /// Allows drag and drop support for this component.
        /// </summary>
        public bool AllowDragDrop { get; set; }

        #region Events

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

            ImGuiNET.ImGui.PushID(Id);

            ApplyStyles();
            UpdateInternal(contentRect);
            RemoveStyles();

            ImGuiNET.ImGui.PopID();

            // Handle Drag and Drop after rendering, so drag drop events go from most nested to least nested control
            if (AllowDragDrop)
                if (Application.Instance.TryGetDragDrop(contentRect, out var dragDrop))
                    OnDragDrop(dragDrop.Event);
        }

        /// <summary>
        /// Gets the finalized width of this component.
        /// </summary>
        /// <param name="parentWidth">The width of the parent component.</param>
        /// <param name="layoutCorrection">A correctional value for layouts.</param>
        /// <returns>The finalized width of this component.</returns>
        public virtual int GetWidth(int parentWidth, float layoutCorrection = 1f)
        {
            return GetDimension(GetSize().Width, parentWidth, layoutCorrection);
        }

        /// <summary>
        /// Gets the finalized height of this component.
        /// </summary>
        /// <param name="parentHeight">The height of the parent component.</param>
        /// <param name="layoutCorrection">A correctional value for layouts.</param>
        /// <returns>The finalized height of this component.</returns>
        public virtual int GetHeight(int parentHeight, float layoutCorrection = 1f)
        {
            return GetDimension(GetSize().Height, parentHeight, layoutCorrection);
        }

        /// <summary>
        /// Gets the absolute or relative size of the component. Use <see cref="GetWidth"/> or <see cref="GetHeight"/> to get the finalized size.
        /// </summary>
        /// <returns>The absolute or relative size of this component.</returns>
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
