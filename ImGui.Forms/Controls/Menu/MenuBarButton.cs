using System;
using ImGui.Forms.Localization;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;
using ImGuiNET;

namespace ImGui.Forms.Controls.Menu
{
    public class MenuBarButton : MenuBarItem
    {
        #region Properties

        public LocalizedString Text { get; set; }

        public FontResource Font { get; set; }

        public KeyCommand KeyAction { get; set; }

        public override int Height => GetHeight();

        #endregion

        #region Events

        public event EventHandler Clicked;

        #endregion

        public MenuBarButton(LocalizedString text = default)
        {
            Text = text;
        }

        protected override void UpdateInternal()
        {
            // Add menu button
            if ((ImGuiNET.ImGui.MenuItem(Text, Enabled) || ImGuiNET.ImGui.IsKeyChordPressed(KeyAction.GetImGuiKeyChord())) && Enabled)
                // Execute click event, if set
                Clicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void UpdateEventsInternal()
        {
            // Add menu button
            if (ImGuiNET.ImGui.IsKeyChordPressed(KeyAction.GetImGuiKeyChord()) && Enabled)
                // Execute click event, if set
                Clicked?.Invoke(this, EventArgs.Empty);
        }

        private int GetHeight()
        {
            ApplyStyles();

            var textSize = TextMeasurer.MeasureText(Text);
            var height = (int)(textSize.Y + ImGuiNET.ImGui.GetStyle().FramePadding.Y * 2);

            RemoveStyles();

            return height;
        }

        protected override void ApplyStyles()
        {
            ImFontPtr? fontPtr = Font?.GetPointer();
            if (fontPtr != null)
                ImGuiNET.ImGui.PushFont(fontPtr.Value);
        }

        protected override void RemoveStyles()
        {
            if (Font?.GetPointer() != null)
                ImGuiNET.ImGui.PopFont();
        }
    }
}
