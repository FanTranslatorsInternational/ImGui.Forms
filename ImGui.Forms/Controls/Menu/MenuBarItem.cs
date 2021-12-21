namespace ImGui.Forms.Controls.Menu
{
    public abstract class MenuBarItem
    {
        /// <summary>
        /// The Id for this component.
        /// </summary>
        protected int Id => Application.Instance.IdFactory.Get(this);

        /// <summary>
        /// The height of the menu bar item.
        /// </summary>
        public abstract int Height { get; }

        /// <summary>
        /// Updates the menu bar component to ImGui.
        /// </summary>
        public void Update()
        {
            ImGuiNET.ImGui.PushID(Id);

            ApplyStyles();
            UpdateInternal();
            RemoveStyles();

            ImGuiNET.ImGui.PopID();
        }

        /// <summary>
        /// Updates the component.
        /// </summary>
        protected abstract void UpdateInternal();

        /// <summary>
        /// Applies any styles specific to this component, before <see cref="UpdateInternal"/> is invoked.
        /// </summary>
        protected virtual void ApplyStyles() { }

        /// <summary>
        /// Removes any styles specific to this component, after <see cref="UpdateInternal"/> is invoked.
        /// </summary>
        protected virtual void RemoveStyles() { }
    }
}
