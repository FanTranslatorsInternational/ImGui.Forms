using System;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
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
            if (ImGuiNET.ImGui.InputTextMultiline($"##{Id}", ref _text, MaxCharacters, contentRect.Size))
                OnTextChanged();
        }

        private void OnTextChanged()
        {
            TextChanged?.Invoke(this, new EventArgs());
        }
    }
}
