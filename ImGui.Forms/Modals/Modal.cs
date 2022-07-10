using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Modals
{
    public class Modal : Component
    {
        private CancellationTokenSource _tokenSource;
        private bool _shouldClose;

        internal Modal ChildModal { get; set; }

        public string Caption { get; set; } = string.Empty;

        public Component Content { get; set; }

        public bool BlockFormClosing { get; private set; }

        protected DialogResult Result { get; set; }

        public Vector2 Size { get; set; } = new Vector2(200, 80);
        public int Width => (int)Size.X;
        public int Height => (int)Size.Y;

        public override Size GetSize()
        {
            return new Size(Width, Height);
        }

        public int GetHeaderHeight()
        {
            return FontResource.GetCurrentLineHeight(withDescent: true) + 6;
        }

        protected override async void UpdateInternal(Rectangle contentRect)
        {
            var id = string.IsNullOrEmpty(Caption) ? "##source" : Caption;
            ImGuiNET.ImGui.OpenPopup(id);

            var exists = true;
            if (ImGuiNET.ImGui.BeginPopupModal(id, ref exists, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize))
            {
                // Create content of popup
                Content?.Update(new Rectangle(contentRect.X, contentRect.Y, contentRect.Width, contentRect.Height));

                // Create content of child modal
                DrawModal(ChildModal);

                // Add closing command to current popup context
                if (_shouldClose)
                    CloseCore();

                ImGuiNET.ImGui.EndPopup();

                if (_shouldClose)
                    await CloseInternal();
            }

            if (!exists)
                _shouldClose = !ShouldCancelClose();
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
            _tokenSource = new CancellationTokenSource();
            try
            {
                await Task.Delay(Timeout.Infinite, _tokenSource.Token);
            }
            catch
            {
                // ignored
            }

            return Result;
        }

        public void Close(DialogResult result)
        {
            Result = result;

            Close();
        }

        public void Close()
        {
            _shouldClose = !ShouldCancelClose();
        }

        // HINT: Only gets executed if _shouldClose is set to true
        private void CloseCore()
        {
            if (Application.Instance?.MainForm != null)
                Application.Instance.MainForm.PopModal();

            _tokenSource?.Cancel();

            ImGuiNET.ImGui.CloseCurrentPopup();
        }

        protected virtual void ShowInternal() { }

        // HINT: Only gets executed if _shouldClose is set to true
        protected virtual Task CloseInternal() => Task.CompletedTask;

        protected virtual bool ShouldCancelClose() => false;

        #region Helper

        internal static void DrawModal(Modal modal)
        {
            if (modal == null)
                return;

            var form = Application.Instance.MainForm;

            var modalPos = new Vector2((form.Width - modal.Width) / 2f, (form.Height - modal.Height - modal.GetHeaderHeight()) / 2f);
            var contentPos = modalPos + new Vector2(form.Padding.X, modal.GetHeaderHeight() + form.Padding.Y);

            var contentSize = new Vector2(modal.Width, modal.Height);
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
