using ImGui.Forms.Models.IO;

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

        internal void UpdateEvents()
        {
            ImGuiNET.ImGui.PushID(Id);

            UpdateEventsInternal();

            ImGuiNET.ImGui.PopID();
        }

        /// <summary>
        /// Updates the component.
        /// </summary>
        protected abstract void UpdateInternal();

        /// <summary>
        /// Executed behaviour based on application events.
        /// </summary>
        protected abstract void UpdateEventsInternal();

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
    }
}
