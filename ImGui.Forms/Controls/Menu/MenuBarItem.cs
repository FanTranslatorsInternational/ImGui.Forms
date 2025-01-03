﻿using ImGui.Forms.Factories;
using ImGui.Forms.Models.IO;

namespace ImGui.Forms.Controls.Menu
{
    public abstract class MenuBarItem
    {
        /// <summary>
        /// The Id for this component.
        /// </summary>
        protected int Id => IdFactory.Get(this);

        /// <summary>
        /// The height of the menu bar item.
        /// </summary>
        public abstract int Height { get; }

        /// <summary>
        /// Determines, if the menu item is enabled.
        /// </summary>
        public virtual bool Enabled { get; set; } = true;

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
    }
}
