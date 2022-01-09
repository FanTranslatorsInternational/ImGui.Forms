using System;
using System.Threading.Tasks;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;

namespace ImGui.Forms.Modals.IO
{
    public class InputBox : Modal
    {
        private const string Ok_ = "Ok";
        private const string Cancel_ = "Cancel";

        private const int ButtonWidth_ = 75;

        private TextBox _textBox;

        public string Input { get; private set; }

        public InputBox(string caption, string text, string placeHolder)
        {
            #region Controls

            var okButton = new Button { Caption = Ok_, Width = ButtonWidth_ };
            var cancelButton = new Button { Caption = Cancel_, Width = ButtonWidth_ };

            _textBox = new TextBox {Placeholder = placeHolder};

            #endregion

            #region Events

            okButton.Clicked += OkButton_Clicked;
            cancelButton.Clicked += CancelButton_Clicked;

            _textBox.TextChanged += TextBox_TextChanged;

            #endregion

            Result = DialogResult.Cancel;
            Caption = caption;

            // TODO: Allow textbox Enter to validate input box
            Content = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Items =
                {
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        ItemSpacing = 4,
                        Items =
                        {
                            new Label {Caption = text},
                            _textBox
                        }
                    },
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        ItemSpacing = 4,
                        Items =
                        {
                            okButton,
                            cancelButton
                        }
                    }
                }
            };
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

        public static async Task<string> ShowAsync(string caption, string text, string placeHolder = "")
        {
            var inputBox = new InputBox(caption, text, placeHolder);
            await inputBox.ShowAsync();

            return inputBox.Input;
        }
    }
}
