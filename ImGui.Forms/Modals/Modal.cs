using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ImGui.Forms.Components.Base;
using ImGui.Forms.Models;
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

        protected DialogResult Result { get; set; }

        public const int HeaderHeight = 20;
        public int Width { get; set; } = 200;
        public int Height { get; set; } = 80;

        public override Size GetSize()
        {
            return new Size(Width, Height);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            var id = string.IsNullOrEmpty(Caption) ? "##source" : Caption;
            ImGuiNET.ImGui.OpenPopup(id);

            var exists = true;
            if (ImGuiNET.ImGui.BeginPopupModal(id, ref exists, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize))
            {
                // Create content of popup
                Content?.Update(new Rectangle(contentRect.X, contentRect.Y, contentRect.Width, contentRect.Height));

                // Create content of child modal
                var modal = ChildModal;
                if (modal != null)
                {
                    var form = Application.Instance.MainForm;

                    var modalPos = new Vector2((form.Width - modal.Width) / 2f - form.Padding.X, (Height - modal.Height) / 2f);
                    var modalContentSize = new Vector2(modal.Width, modal.Height);
                    var modalSize = modalContentSize + new Vector2(form.Padding.X * 2, HeaderHeight + form.Padding.Y * 2);

                    ImGuiNET.ImGui.SetNextWindowPos(modalPos);
                    ImGuiNET.ImGui.SetNextWindowSize(modalSize);
                    modal.Update(new Rectangle((int)modalPos.X, (int)modalPos.Y, (int)modalContentSize.X, (int)modalContentSize.Y));
                }

                // Add closing command to current popup context
                if(_shouldClose)
                    CloseCore();

                ImGuiNET.ImGui.EndPopup();
            }

            if (!exists)
                _shouldClose = true;
        }

        public async Task<DialogResult> ShowAsync()
        {
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

        public void Close()
        {
            _shouldClose = true;
        }

        private void CloseCore()
        {
            ImGuiNET.ImGui.CloseCurrentPopup();

            CloseWithoutImGui();
        }

        private void CloseWithoutImGui()
        {
            if (Application.Instance?.MainForm != null)
                Application.Instance.MainForm.PopModal();

            _tokenSource?.Cancel();
            CloseInternal();
        }

        protected virtual void ShowInternal() { }

        protected virtual void CloseInternal() { }
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
