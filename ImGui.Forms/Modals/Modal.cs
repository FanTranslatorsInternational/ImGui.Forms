using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;
using ImGui.Forms.Support;

namespace ImGui.Forms.Modals;

public abstract class Modal : Component
{
    private static readonly KeyCommand CloseCommand = new(ImGuiKey.Escape);

    private CancellationTokenSource? _tokenSource;
    private bool _shouldClose;

    internal Modal? ChildModal { get; set; }

    public LocalizedString Caption { get; set; } = string.Empty;

    public ModalMenuBar? MenuBar { get; set; }
    public Component? Content { get; set; }

    public bool BlockFormClosing { get; private set; }

    public KeyCommand OkAction { get; set; }
    public KeyCommand CancelAction { get; set; }

    protected DialogResult Result { get; set; }

    public Size Size { get; set; } = new(SizeValue.Absolute(200), SizeValue.Absolute(80));

    public override Size GetSize() => Size;

    protected override async void UpdateInternal(Rectangle contentRect)
    {
        var id = Caption.IsEmpty ? "##source" : (string)Caption;
        Hexa.NET.ImGui.ImGui.OpenPopup(id);

        Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.PopupBg, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.WindowBg));

        var flags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
        if (MenuBar is not null)
            flags |= ImGuiWindowFlags.MenuBar;

        var exists = true;
        var shows = Hexa.NET.ImGui.ImGui.BeginPopupModal(id, ref exists, flags);
        if (shows)
        {
            // Create menu bar of popup
            MenuBar?.Update();
            var menuHeight = MenuBar?.Height ?? 0;

            // Create content of popup
            Content?.Update(new Rectangle(contentRect.Position + new Vector2(0, menuHeight), contentRect.Size - new Vector2(0, menuHeight)));

            if (OkAction.IsPressed())
                Close(DialogResult.Ok);
            else if (CancelAction.IsPressed() || CloseCommand.IsPressed())
                Close(DialogResult.Cancel);

            // Create content of child modal
            DrawModal(ChildModal);

            Hexa.NET.ImGui.ImGui.EndPopup();
        }

        Hexa.NET.ImGui.ImGui.PopStyleColor();

        if (shows && _shouldClose)
            await CloseCore();

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
        return TextMeasurer.GetCurrentLineHeight() + 6;
    }

    // HINT: Only gets executed if _shouldClose is set to true
    private async Task CloseCore()
    {
        if (Application.Instance?.MainForm != null)
            Application.Instance.MainForm.PopModal();

        await CloseInternal();

        await _tokenSource?.CancelAsync()!;

        Hexa.NET.ImGui.ImGui.CloseCurrentPopup();
    }

    protected virtual void ShowInternal() { }

    // HINT: Only gets executed if _shouldClose is set to true
    protected virtual Task CloseInternal() => Task.CompletedTask;

    protected virtual Task<bool> ShouldCancelClose() => Task.FromResult(false);

    #region Helper

    internal static void DrawModal(Modal? modal)
    {
        if (modal?.Content == null)
            return;

        var form = Application.Instance?.MainForm;
        if (form == null)
            return;

        form.SetRenderingModal(modal);

        var modalWidth = modal.Size.Width.IsContentAligned ? modal.Content.GetWidth(form.Width, form.Height) : GetDimension(modal.Size.Width, form.Width);
        var modalHeight = modal.Size.Height.IsContentAligned ? modal.Content.GetHeight(form.Width, form.Height) : GetDimension(modal.Size.Height, form.Height);

        var modalPos = new Vector2((form.Width - modalWidth) / 2f, (form.Height - modalHeight - modal.GetHeaderHeight()) / 2f);
        var contentPos = modalPos + form.Padding with { Y = modal.GetHeaderHeight() + form.Padding.Y };

        var contentSize = new Vector2(modalWidth, modalHeight);
        var modalSize = contentSize + new Vector2(form.Padding.X * 2, modal.GetHeaderHeight() + form.Padding.Y * 2);

        Hexa.NET.ImGui.ImGui.SetNextWindowPos(modalPos);
        Hexa.NET.ImGui.ImGui.SetNextWindowSize(modalSize);

        modal.Update(new Rectangle(contentPos, contentSize));
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