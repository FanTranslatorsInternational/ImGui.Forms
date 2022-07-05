using System;
using System.Numerics;
using System.Threading.Tasks;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Models;

namespace ImGui.Forms.Modals.IO
{
    public class InputBox : Modal
    {
        private const string Ok_ = "Ok";
        private const string Cancel_ = "Cancel";

        private const int ButtonWidth_ = 75;

        private readonly TextBox _textBox;

        public string Input { get; private set; }

        private InputBox(string caption, string text, string preset, string placeHolder)
        {
            #region Controls

            var okButton = new Button { Caption = Ok_, Width = ButtonWidth_ };
            var cancelButton = new Button { Caption = Cancel_, Width = ButtonWidth_ };

            var label = new Label { Caption = text };
            _textBox = new TextBox { Placeholder = placeHolder };

            #endregion

            #region Events

            okButton.Clicked += OkButton_Clicked;
            cancelButton.Clicked += CancelButton_Clicked;

            _textBox.TextChanged += TextBox_TextChanged;

            #endregion

            _textBox.Text = preset;

            Result = DialogResult.Cancel;
            Caption = caption;

            Content = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Size = new Size(1f, -1),
                Items =
                {
                    label,
                    _textBox,
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        ItemSpacing = 4,
                        Size=new Size(1f,-1),
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

        public static async Task<string> ShowAsync(string caption, string text, string preset = "", string placeHolder = "")
        {
            var inputBox = new InputBox(caption, text, preset, placeHolder);
            await inputBox.ShowAsync();

            return inputBox.Result == DialogResult.Cancel ? null : inputBox.Input;
        }
    }
}
