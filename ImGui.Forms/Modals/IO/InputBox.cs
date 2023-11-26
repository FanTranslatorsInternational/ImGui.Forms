using System;
using System.Numerics;
using System.Threading.Tasks;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using Veldrid;

namespace ImGui.Forms.Modals.IO
{
    public class InputBox : Modal
    {
        private const string Ok_ = "Ok";
        private const string Cancel_ = "Cancel";

        private const int ButtonWidth_ = 75;

        private readonly TextBox _textBox;

        public string Input { get; private set; }

        private InputBox(LocalizedString caption, LocalizedString text, string preset, LocalizedString? placeHolder, int maxCharacters)
        {
            #region Controls

            var okButton = new Button { Text = Ok_, Width = ButtonWidth_ };
            var cancelButton = new Button { Text = Cancel_, Width = ButtonWidth_ };

            var label = new Label { Text = text };

            _textBox = new TextBox();
            if (placeHolder != null)
                _textBox.Placeholder = placeHolder.Value;
            if (maxCharacters >= 0)
                _textBox.MaxCharacters = (uint)maxCharacters;

            #endregion

            #region Events

            okButton.Clicked += OkButton_Clicked;
            cancelButton.Clicked += CancelButton_Clicked;

            _textBox.TextChanged += TextBox_TextChanged;

            #endregion

            #region Keys

            OkAction = new KeyCommand(ModifierKeys.None, Key.Enter);
            CancelAction = new KeyCommand(ModifierKeys.None, Key.Escape);

            #endregion

            _textBox.Text = preset;

            Result = DialogResult.Cancel;
            Caption = caption;

            Content = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Size = new Size(SizeValue.Parent, SizeValue.Content),
                Items =
                {
                    label,
                    _textBox,
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        ItemSpacing = 4,
                        Size=new Size(SizeValue.Parent, SizeValue.Content),
                        Items =
                        {
                            okButton,
                            cancelButton
                        }
                    }
                }
            };

            var width = Application.Instance.MainForm.Width * .8f;
            var height = Content.GetHeight(Application.Instance.MainForm.Height);
            Size = new Vector2(width, height);
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            Input = _textBox.Text;
        }

        private void CancelButton_Clicked(object sender, EventArgs e)
        {
            Result = DialogResult.Cancel;
            Close();
        }

        private void OkButton_Clicked(object sender, EventArgs e)
        {
            Result = DialogResult.Ok;
            Close();
        }

        public static async Task<string> ShowAsync(LocalizedString caption, LocalizedString text, string preset = "", LocalizedString? placeHolder = null, int maxCharacters = -1)
        {
            var inputBox = new InputBox(caption, text, preset, placeHolder, maxCharacters);
            await inputBox.ShowAsync();

            return inputBox.Result == DialogResult.Cancel ? null : inputBox.Input;
        }
    }
}
