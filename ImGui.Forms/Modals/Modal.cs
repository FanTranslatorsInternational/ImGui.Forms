using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Modals
{
    public abstract class Modal : Component
    {
        private static readonly KeyCommand CloseCommand = new(Key.Escape);

        private CancellationTokenSource _tokenSource;
        private bool _shouldClose;

        internal Modal ChildModal { get; set; }

        public LocalizedString Caption { get; set; } = string.Empty;

        public Component Content { get; set; }

        public bool BlockFormClosing { get; private set; }

        public KeyCommand OkAction { get; set; }
        public KeyCommand CancelAction { get; set; }

        protected DialogResult Result { get; set; }

        public Size Size { get; set; } = new(SizeValue.Absolute(200), SizeValue.Absolute(80));

        public override Size GetSize() => Size;

        protected override async void UpdateInternal(Rectangle contentRect)
        {
            var id = Caption.IsEmpty ? "##source" : (string)Caption;
            ImGuiNET.ImGui.OpenPopup(id);

            ImGuiNET.ImGui.PushStyleColor(ImGuiCol.PopupBg, ImGuiNET.ImGui.GetColorU32(ImGuiCol.WindowBg));

            var exists = true;
            if (ImGuiNET.ImGui.BeginPopupModal(id, ref exists, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize))
            {
                // Create content of popup
                Content?.Update(new Rectangle(contentRect.X, contentRect.Y, contentRect.Width, contentRect.Height));

                if (OkAction.IsPressed())
                    Close(DialogResult.Ok);
                else if (CancelAction.IsPressed() || CloseCommand.IsPressed())
                    Close(DialogResult.Cancel);

                // Create content of child modal
                DrawModal(ChildModal);

                ImGuiNET.ImGui.EndPopup();

                if (_shouldClose)
                    await CloseCore();
            }

            ImGuiNET.ImGui.PopStyleColor();

            if (!exists)
                Close();
        }

        public async Task<DialogResult> ShowAsync(bool blockFormClosing = false)
        {
            BlockFormClosing = blockFormClosing;

            if (Application.Instance?.MainForm == null || _tokenSource != null)
                return DialogResult.None;

            // Add modal to rendering pipeline
            Application.Instance.MainForm.PushModal(this);

            // Execute code from the inherited class
            ShowInternal();

            // Wait for modal to be closed
            _shouldClose = false;
            _tokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(Timeout.Infinite, _tokenSource.Token);
            }
            catch
            {
                // ignored
            }

            _tokenSource = null;

            return Result;
        }

        public void Close(DialogResult result)
        {
            Result = result;

            Close();
        }

        public async void Close()
        {
            _shouldClose = !await ShouldCancelClose();
        }

        private float GetHeaderHeight()
        {
            return TextMeasurer.GetCurrentLineHeight(withDescent: true) + 6;
        }

        // HINT: Only gets executed if _shouldClose is set to true
        private async Task CloseCore()
        {
            if (Application.Instance?.MainForm != null)
                Application.Instance.MainForm.PopModal();

            await CloseInternal();

            await _tokenSource?.CancelAsync()!;

            ImGuiNET.ImGui.CloseCurrentPopup();
        }

        protected virtual void ShowInternal() { }

        // HINT: Only gets executed if _shouldClose is set to true
        protected virtual Task CloseInternal() => Task.CompletedTask;

        protected virtual Task<bool> ShouldCancelClose() => Task.FromResult(false);

        #region Helper

        internal static void DrawModal(Modal modal)
        {
            if (modal == null)
                return;

            Application.Instance.MainForm.SetRenderingModal(modal);

            var form = Application.Instance.MainForm;

            var modalWidth = GetDimension(modal.Size.Width, form.Width);
            var modalHeight = GetDimension(modal.Size.Height, form.Height);

            var modalPos = new Vector2((form.Width - modalWidth) / 2f, (form.Height - modalHeight - modal.GetHeaderHeight()) / 2f);
            var contentPos = modalPos + new Vector2(form.Padding.X, modal.GetHeaderHeight() + form.Padding.Y);

            var contentSize = new Vector2(modalWidth, modalHeight);
            var modalSize = contentSize + new Vector2(form.Padding.X * 2, modal.GetHeaderHeight() + form.Padding.Y * 2);

            ImGuiNET.ImGui.SetNextWindowPos(modalPos);
            ImGuiNET.ImGui.SetNextWindowSize(modalSize);

            modal.Update(new Rectangle((int)contentPos.X, (int)contentPos.Y, (int)contentSize.X, (int)contentSize.Y));
        }

        #endregion
    }

    public enum DialogResult
    {
        None,
        Ok,
        Cancel,
        Yes,
        No
    }
}
