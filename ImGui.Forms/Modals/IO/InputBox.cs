using System;
using System.Threading.Tasks;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;
using Veldrid;

namespace ImGui.Forms.Modals.IO;

public class InputBox : Modal
{
    private const int ButtonWidth_ = 75;

    private readonly TextBox _textBox;

    public string Input { get; private set; }

    private InputBox(LocalizedString caption, LocalizedString text, string preset, LocalizedString? placeholder, int maxCharacters)
    {
        #region Controls

        var okButton = new Button { Text = LocalizationResources.Ok(), Width = ButtonWidth_ };
        var cancelButton = new Button { Text = LocalizationResources.Cancel(), Width = ButtonWidth_ };

        var label = new Label { Text = text };

        _textBox = new TextBox();
        if (placeholder != null)
            _textBox.Placeholder = placeholder.Value;
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
            Size = Size.WidthAlign,
            Items =
            {
                label,
                _textBox,
                new StackLayout
                {
                    Alignment = Alignment.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    ItemSpacing = 4,
                    Size=Size.WidthAlign,
                    Items =
                    {
                        okButton,
                        cancelButton
                    }
                }
            }
        };

        var height = Content.GetHeight(Application.Instance.MainForm.Width, Application.Instance.MainForm.Height);
        Size = new Size(SizeValue.Relative(.8f), SizeValue.Absolute(height));
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