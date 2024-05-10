using System;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class MultiLineTextBox : Component
    {
        private string _text = string.Empty;

        #region Properties

        /// <summary>
        /// The text that was set or changed in this component.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                _text = value ?? string.Empty;
                OnTextChanged();
            }
        }

        /// <summary>
        /// Marks the input as read-only.
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Get or set the max count of characters allowed in the text box.
        /// </summary>
        public uint MaxCharacters { get; set; } = 2048;

        #endregion

        #region Events

        public event EventHandler TextChanged;

        #endregion

        public override Size GetSize() => Size.Parent;

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var enabled = Enabled;
            var isReadonly = IsReadOnly;

            var flags = ImGuiInputTextFlags.None;
            if (isReadonly || !enabled) flags |= ImGuiInputTextFlags.ReadOnly;

            if (isReadonly || !enabled)
            {
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGuiNET.ImGui.GetColorU32(ImGuiCol.TextDisabled));
            }

            if (ImGuiNET.ImGui.InputTextMultiline($"##{Id}", ref _text, MaxCharacters, contentRect.Size, flags))
                OnTextChanged();

            if (isReadonly || !enabled)
                ImGuiNET.ImGui.PopStyleColor(3);
        }

        private void OnTextChanged()
        {
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
