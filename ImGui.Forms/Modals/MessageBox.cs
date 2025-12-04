using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;

namespace ImGui.Forms.Modals;

public class MessageBox : Modal
{
    private const int ButtonWidth_ = 75;

    private MessageBox(LocalizedString caption, LocalizedString text, MessageBoxType type, MessageBoxButton buttons)
    {
        CreateLayout(caption, text, type, buttons);

        if (buttons.HasFlag(MessageBoxButton.Cancel))
            Result = DialogResult.Cancel;
        else if (buttons.HasFlag(MessageBoxButton.No))
            Result = DialogResult.No;
        else if (buttons.HasFlag(MessageBoxButton.Ok))
            Result = DialogResult.Ok;
    }

    #region Static accessors

    public static Task<DialogResult> ShowErrorAsync(LocalizedString caption = default, LocalizedString text = default, bool blockFormClosing = false)
    {
        return ShowAsync(caption, text, MessageBoxType.Error, blockFormClosing: blockFormClosing);
    }

    public static Task<DialogResult> ShowInformationAsync(LocalizedString caption = default, LocalizedString text = default, bool blockFormClosing = false)
    {
        return ShowAsync(caption, text, blockFormClosing: blockFormClosing);
    }

    public static Task<DialogResult> ShowYesNoAsync(LocalizedString caption = default, LocalizedString text = default, bool blockFormClosing = false)
    {
        return ShowAsync(caption, text, MessageBoxType.Warning, MessageBoxButton.Yes | MessageBoxButton.No, blockFormClosing);
    }

    public static Task<DialogResult> ShowYesNoCancelAsync(LocalizedString caption = default, LocalizedString text = default, bool blockFormClosing = false)
    {
        return ShowAsync(caption, text, MessageBoxType.Warning, MessageBoxButton.Yes | MessageBoxButton.No | MessageBoxButton.Cancel, blockFormClosing);
    }

    private static async Task<DialogResult> ShowAsync(LocalizedString caption = default, LocalizedString text = default, MessageBoxType type = MessageBoxType.Information, MessageBoxButton buttons = MessageBoxButton.Ok, bool blockFormClosing = false)
    {
        // Even though modal.Show already checks for null, we do that here, so the layout is not created only to be disposed again, when the Modal is about to be shown
        if (Application.Instance?.MainForm == null)
            return DialogResult.None;

        var msgBox = new MessageBox(caption, text, type, buttons);
        await msgBox.ShowAsync(blockFormClosing);

        return msgBox.Result;
    }

    #endregion

    private void CreateLayout(LocalizedString caption, LocalizedString text, MessageBoxType type, MessageBoxButton buttons)
    {
        Caption = caption;

        // Prepare message layout
        var msgType = GetTypeImage(type);
        var msgLabel = new Label { Text = text };

        var messageLayout = new StackLayout { Alignment = Alignment.Horizontal, Size = Size.Content, ItemSpacing = 5 };
        if (msgType != null)
            messageLayout.Items.Add(new StackItem(msgType) { VerticalAlignment = VerticalAlignment.Center });
        messageLayout.Items.Add(new StackItem(msgLabel) { VerticalAlignment = VerticalAlignment.Center });

        // Prepare buttons
        var buttonLayout = new StackLayout { Alignment = Alignment.Horizontal, Size = Size.Content, ItemSpacing = 5 };
        foreach (var button in GetButtons(buttons))
            buttonLayout.Items.Add(button);

        // Prepare main layout
        var mainLayout = new StackLayout { Alignment = Alignment.Vertical, Size = Size.Content, ItemSpacing = 5 };
        mainLayout.Items.Add(messageLayout);
        mainLayout.Items.Add(new StackItem(buttonLayout) { HorizontalAlignment = HorizontalAlignment.Right });

        // Add modal
        var mainSize = Application.Instance.MainForm.Size;

        var width = mainLayout.GetWidth((int)mainSize.X, (int)mainSize.Y) + (msgType?.GetWidth((int)mainSize.X, (int)mainSize.Y) ?? 0) + messageLayout.ItemSpacing;
        var height = mainLayout.GetHeight((int)mainSize.X, (int)mainSize.Y);

        Size = new Size(SizeValue.Absolute(width), SizeValue.Absolute(height));
        Content = mainLayout;
    }

    private Component GetTypeImage(MessageBoxType type)
    {
        ThemedImageResource image;
        switch (type)
        {
            case MessageBoxType.Information:
                return null;

            case MessageBoxType.Warning:
                return null;

            case MessageBoxType.Error:
                image = ImageResources.Error;
                break;

            default:
                return null;
        }

        return new PictureBox(image);
    }

    private IEnumerable<Button> GetButtons(MessageBoxButton buttons)
    {
        if (buttons.HasFlag(MessageBoxButton.Ok))
        {
            var okButton = new Button { Text = LocalizationResources.Ok(), Width = SizeValue.Absolute(ButtonWidth_) };
            okButton.Clicked += (_, _) => Close();

            yield return okButton;
        }

        if (buttons.HasFlag(MessageBoxButton.Yes))
        {
            var yesButton = new Button { Text = LocalizationResources.Yes(), Width = SizeValue.Absolute(ButtonWidth_) };
            yesButton.Clicked += (_, _) =>
            {
                Result = DialogResult.Yes;
                Close();
            };

            yield return yesButton;
        }

        if (buttons.HasFlag(MessageBoxButton.No))
        {
            var noButton = new Button { Text = LocalizationResources.No(), Width = SizeValue.Absolute(ButtonWidth_) };
            noButton.Clicked += (_, _) =>
            {
                Result = DialogResult.No;
                Close();
            };

            yield return noButton;
        }

        if (buttons.HasFlag(MessageBoxButton.Cancel))
        {
            var cancelButton = new Button { Text = LocalizationResources.Cancel(), Width = SizeValue.Absolute(ButtonWidth_) };
            cancelButton.Clicked += (_, _) =>
            {
                Result = DialogResult.Cancel;
                Close();
            };

            yield return cancelButton;
        }
    }
}

public enum MessageBoxType
{
    Information,
    Warning,
    Error
}

[Flags]
public enum MessageBoxButton
{
    Ok = 1,
    Yes = 2,
    No = 4,
    Cancel = 8
}