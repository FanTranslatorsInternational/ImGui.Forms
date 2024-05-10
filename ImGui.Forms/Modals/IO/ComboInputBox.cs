using System;
using System.Linq;
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
    public class ComboInputBox : Modal
    {
        private const string Ok_ = "Ok";
        private const string Cancel_ = "Cancel";

        private const int ButtonWidth_ = 75;

        private readonly ComboBox<LocalizedString> _comboBox;

        public LocalizedString SelectedItem { get; private set; }

        private ComboInputBox(LocalizedString caption, LocalizedString text, LocalizedString[] items, LocalizedString? preset)
        {
            #region Controls

            var okButton = new Button { Text = Ok_, Width = ButtonWidth_ };
            var cancelButton = new Button { Text = Cancel_, Width = ButtonWidth_ };

            var label = new Label { Text = text };

            _comboBox = new ComboBox<LocalizedString>();
            foreach (LocalizedString item in items)
                _comboBox.Items.Add(item);

            #endregion

            #region Events

            okButton.Clicked += OkButton_Clicked;
            cancelButton.Clicked += CancelButton_Clicked;

            _comboBox.SelectedItemChanged += ComboBox_SelectedItemChanged;

            #endregion

            #region Keys

            OkAction = new KeyCommand(ModifierKeys.None, Key.Enter);
            CancelAction = new KeyCommand(ModifierKeys.None, Key.Escape);

            #endregion

            _comboBox.SelectedItem = _comboBox.Items.FirstOrDefault(i => i.Content == preset);
            SelectedItem = _comboBox.SelectedItem!.Content;

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
                    _comboBox,
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

        private void ComboBox_SelectedItemChanged(object sender, EventArgs e)
        {
            SelectedItem = _comboBox.SelectedItem.Content;
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

        public static async Task<LocalizedString?> ShowAsync(LocalizedString caption, LocalizedString text, LocalizedString[] items, LocalizedString? preset = null)
        {
            if (items.Length <= 0)
                return null;

            if (preset == null || !items.Contains(preset.Value))
                preset = items.First();

            var inputBox = new ComboInputBox(caption, text, items, preset);
            DialogResult result = await inputBox.ShowAsync();
            if (result != DialogResult.Ok)
                return null;

            return inputBox.SelectedItem;
        }
    }
}
