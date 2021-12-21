using System;
using System.Numerics;
using ImGui.Forms.Components.Base;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Components
{
    public class TextBox : Component
    {
        private bool _activePreviousFrame = false;
        private string _text = string.Empty;

        /// <summary>
        /// Get or set the text shown.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnTextChanged();
            }
        }

        public SizeValue Width { get; set; } = SizeValue.Relative(1f);

        public Vector2 Padding { get; set; } = new Vector2(2, 2);

        public FontResource FontResource { get; set; }

        /// <summary>
        /// Masks the input text, for security reasons. Eg passwords.
        /// </summary>
        public bool IsMasked { get; set; }

        /// <summary>
        /// Marks the input as read-only.
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Get or set the restriction on input characters in the text box.
        /// </summary>
        public CharacterRestriction AllowedCharacters { get; set; }

        /// <summary>
        /// Get or set the max count of characters allowed in the text box.
        /// </summary>
        public uint MaxCharacters { get; set; } = 256;

        public string Placeholder { get; set; }

        #region Events

        public event EventHandler TextChanged;
        public event EventHandler FocusLost;

        #endregion

        public override Size GetSize()
        {
            ApplyStyles();

            var textSize = ImGuiNET.ImGui.CalcTextSize(_text ?? string.Empty);
            SizeValue width = (int)Width.Value == -1 ? (int)Math.Ceiling(textSize.X) + (int)Padding.X * 2 : Width;
            var height = (int)Padding.Y * 2 + (int)Math.Ceiling(textSize.Y);

            RemoveStyles();

            return new Size(width, height);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var flags = ImGuiInputTextFlags.None;
            if (IsMasked) flags |= ImGuiInputTextFlags.Password;
            if (IsReadOnly) flags |= ImGuiInputTextFlags.ReadOnly;

            switch (AllowedCharacters)
            {
                case CharacterRestriction.Decimal:
                    flags |= ImGuiInputTextFlags.CharsDecimal;
                    break;

                case CharacterRestriction.Hexadecimal:
                    flags |= ImGuiInputTextFlags.CharsHexadecimal;
                    break;
            }

            ImGuiNET.ImGui.SetNextItemWidth(contentRect.Width);

            if (!string.IsNullOrEmpty(Placeholder))
            {
                if (ImGuiNET.ImGui.InputTextWithHint($"##{Id}", Placeholder, ref _text, MaxCharacters, flags))
                    OnTextChanged();

                return;
            }

            if (ImGuiNET.ImGui.InputText($"##{Id}", ref _text, MaxCharacters, flags))
                OnTextChanged();

            // Check if InputText is active and lost focus
            if (!ImGuiNET.ImGui.IsItemActive() && _activePreviousFrame)
                OnFocusLost();

            _activePreviousFrame = ImGuiNET.ImGui.IsItemActive();
        }

        protected override void ApplyStyles()
        {
            if (FontResource != null)
                ImGuiNET.ImGui.PushFont((ImFontPtr)FontResource);

            ImGuiNET.ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Padding);
        }

        protected override void RemoveStyles()
        {
            ImGuiNET.ImGui.PopStyleVar();

            if (FontResource != null)
                ImGuiNET.ImGui.PopFont();
        }

        private void OnTextChanged()
        {
            TextChanged?.Invoke(this, new EventArgs());
        }

        private void OnFocusLost()
        {
            FocusLost?.Invoke(this, new EventArgs());
        }
    }

    public enum CharacterRestriction
    {
        Any,
        Decimal,
        Hexadecimal
    }
}
