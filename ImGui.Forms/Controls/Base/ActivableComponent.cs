using System;

namespace ImGui.Forms.Controls.Base
{
    public abstract class ActivableComponent : Component
    {
        /// <summary>
        /// Gets or sets if the component should be active.
        /// </summary>
        public bool Active { get; set; }

        internal event EventHandler Activated;

        /// <summary>
        /// Receive the absolute activation state for this component.
        /// </summary>
        /// <returns></returns>
        /// <remarks>This method is preferred over <see cref="Active"/> since it reflects changes by <see cref="ToggleActive"/>.</remarks>
        public bool IsActive() => Enabled && (Active || IsActiveCore());

        /// <summary>
        /// Toggle the state of activity for the component based on <paramref name="toggleActive"/>.
        /// </summary>
        /// <param name="toggleActive">The state of activity for the component at this frame.</param>
        /// <returns>The state of activity for the component at this frame.</returns>
        protected bool ToggleActive(bool toggleActive)
        {
            Active = toggleActive;

            if (toggleActive)
                OnActivated();

            return IsActive();
        }

        private void OnActivated()
        {
            Activated?.Invoke(this, EventArgs.Empty);
        }
    }
}
