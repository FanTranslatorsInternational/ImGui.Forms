using System;
using System.Linq;
using System.Threading.Tasks;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;
using Veldrid;

namespace ImGui.Forms.Modals.IO
{
    public class ComboInputBox : Modal
    {
        private const int ButtonWidth_ = 75;

        private readonly ComboBox<LocalizedString> _comboBox;

        public LocalizedString SelectedItem { get; private set; }

        private ComboInputBox(LocalizedString caption, LocalizedString text, LocalizedString[] items, LocalizedString? preset)
        {
            #region Controls

            var okButton = new Button { Text = LocalizationResources.Ok(), Width = ButtonWidth_ };
            var cancelButton = new Button { Text = LocalizationResources.Cancel(), Width = ButtonWidth_ };

            var label = new Label { Text = text };

            _comboBox = new ComboBox<LocalizedString> { MaxShowItems = 2 };
            foreach (LocalizedString item in items)
                _comboBox.Items.Add(item);

            var buttonLayout = new StackLayout { Alignment = Alignment.Horizontal, Size = Size.Content, ItemSpacing = 5 };
            buttonLayout.Items.Add(okButton);
            buttonLayout.Items.Add(cancelButton);

            var mainLayout = new StackLayout { Alignment = Alignment.Vertical, Size = Size.Content, ItemSpacing = 5 };
            mainLayout.Items.Add(label);
            mainLayout.Items.Add(_comboBox);
            mainLayout.Items.Add(new StackItem(buttonLayout) { HorizontalAlignment = HorizontalAlignment.Right });

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

            Content = mainLayout;

            var mainSize = Application.Instance.MainForm.Size;

            var width = mainLayout.GetWidth((int)mainSize.X, (int)mainSize.Y);
            var height = mainLayout.GetHeight((int)mainSize.X, (int)mainSize.Y);

            Size = new Size(SizeValue.Absolute(width), SizeValue.Absolute(height));
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
