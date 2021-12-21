using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Models;

namespace ImGui.Forms.Modals
{
    public class MessageBox : Modal
    {
        private MessageBox(string caption, string text, MessageBoxType type, MessageBoxButton buttons)
        {
            CreateLayout(caption, text, type, buttons);

            if (buttons.HasFlag(MessageBoxButton.No))
                Result = DialogResult.No;
            else if (buttons.HasFlag(MessageBoxButton.Ok))
                Result = DialogResult.Ok;
        }

        #region Static accessors

        public static Task<DialogResult> ShowErrorAsync(string caption = "", string text = "")
        {
            return ShowAsync(caption, text, MessageBoxType.Error);
        }

        public static Task<DialogResult> ShowInformationAsync(string caption = "", string text = "")
        {
            return ShowAsync(caption, text);
        }

        public static Task<DialogResult> ShowYesNoAsync(string caption = "", string text = "")
        {
            //return ShowInformationAsync(caption, text);
            return ShowAsync(caption, text, MessageBoxType.Warning, MessageBoxButton.Yes | MessageBoxButton.No);
        }

        private static async Task<DialogResult> ShowAsync(string caption = "", string text = "", MessageBoxType type = MessageBoxType.Information, MessageBoxButton buttons = MessageBoxButton.Ok)
        {
            // Even though modal.Show already checks for null, we do that here, so the layout is not created only to be disposed again, when the Modal is about to be shown
            if (Application.Instance?.MainForm == null)
                return DialogResult.None;

            var msgBox = new MessageBox(caption, text, type, buttons);
            await ((Modal)msgBox).ShowAsync();

            return msgBox.Result;
        }

        #endregion

        private void CreateLayout(string caption, string text, MessageBoxType type, MessageBoxButton buttons)
        {
            Caption = caption;

            // Prepare message layout
            var msgType = GetTypeImage(type);
            var msgLabel = new Label { Caption = text };

            var messageLayout = new StackLayout { Alignment = Alignment.Horizontal, Size = Models.Size.Content, ItemSpacing = 5 };
            if (msgType != null)
                messageLayout.Items.Add(new StackItem(msgType) { VerticalAlignment = VerticalAlignment.Center });
            messageLayout.Items.Add(new StackItem(msgLabel) { VerticalAlignment = VerticalAlignment.Center });

            // Prepare buttons
            var buttonLayout = new StackLayout { Alignment = Alignment.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Size = new Models.Size(1f, -1), ItemSpacing = 5 };
            foreach (var button in GetButtons(buttons))
                buttonLayout.Items.Add(button);

            // Prepare main layout
            var mainLayout = new StackLayout { Alignment = Alignment.Vertical, ItemSpacing = 5, Size = Models.Size.Content };
            mainLayout.Items.Add(new StackItem(messageLayout));
            mainLayout.Items.Add(buttonLayout);

            // Add modal
            var mainSize = new Vector2(Application.Instance.MainForm.Width, Application.Instance.MainForm.Height);

            Width = msgLabel.GetWidth((int)mainSize.X) + (msgType?.GetWidth((int)mainSize.X) ?? 0) + messageLayout.ItemSpacing;
            Height =  mainLayout.GetHeight((int)mainSize.Y);
            Content = mainLayout;
        }

        private Component GetTypeImage(MessageBoxType type)
        {
            Stream resource;
            switch (type)
            {
                case MessageBoxType.Information:
                    return null;

                case MessageBoxType.Warning:
                    return null;

                case MessageBoxType.Error:
                    resource = typeof(MessageBox).Assembly.GetManifestResourceStream("imGui.Forms.Resources.Images.error.png");
                    break;

                default:
                    return null;
            }

            return new PictureBox { Image = new ImageResource((Bitmap)Image.FromStream(resource)) };
        }

        private IEnumerable<Button> GetButtons(MessageBoxButton buttons)
        {
            if (buttons.HasFlag(MessageBoxButton.Ok))
            {
                var okButton = new Button { Caption = "Ok", Padding = new Vector2(30, 2) };
                okButton.Clicked += (s, e) => Close();

                yield return okButton;
            }
            if (buttons.HasFlag(MessageBoxButton.Yes))
            {
                var yesButton = new Button { Caption = "Yes", Padding = new Vector2(25, 2) };
                yesButton.Clicked += (s, e) =>
                {
                    Result = DialogResult.Yes;
                    Close();
                };

                yield return yesButton;
            }
            if (buttons.HasFlag(MessageBoxButton.No))
            {
                var noButton = new Button { Caption = "No", Padding = new Vector2(30, 2) };
                noButton.Clicked += (s, e) =>
                {
                    Result = DialogResult.No;
                    Close();
                };

                yield return noButton;
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
        No = 4
    }
}
