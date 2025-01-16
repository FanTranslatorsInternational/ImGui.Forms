using ImGui.Forms.Localization;

namespace ImGui.Forms.Resources
{
    static class LocalizationResources
    {
        private const string Ok_ = "Ok";
        private const string Cancel_ = "Cancel";
        private const string Yes_ = "Yes";
        private const string No_ = "No";
        private const string Save_ = "Save";
        private const string SearchPlaceholder_ = "Search...";
        private const string ItemName_ = "Name";
        private const string ItemType_ = "Type";
        private const string ItemDateModified_ = "Date Modified";
        private const string SelectedFile_ = "File Name:";
        private const string ReplaceFileCaption_ = "File exists";
        private const string ReplaceFileText_ = "Do you want to overwrite file {0}?";
        private const string CreateFolderCaption_ = "Create folder";
        private const string CreateFolderText_ = "New folder name:";

        private const string OkIdentifier_ = "ImGui.Button.Ok";
        private const string CancelIdentifier_ = "ImGui.Button.Cancel";
        private const string YesIdentifier_ = "ImGui.Button.Yes";
        private const string NoIdentifier_ = "ImGui.Button.No";
        private const string SaveIdentifier_ = "ImGui.Button.Save";
        private const string SearchIdentifier_ = "ImGui.FileDialog.Search";
        private const string ItemNameIdentifier_ = "ImGui.FileDialog.Name";
        private const string ItemTypeIdentifier_ = "ImGui.FileDialog.Type";
        private const string ItemDateModifiedIdentifier_ = "ImGui.FileDialog.DateModified";
        private const string SelectedFileIdentifier_ = "ImGui.FileDialog.SelectedFile";
        private const string ReplaceFileCaptionIdentifier_ = "ImGui.FileDialog.ReplaceFile.Caption";
        private const string ReplaceFileTextIdentifier_ = "ImGui.FileDialog.ReplaceFile.Text";
        private const string CreateFolderCaptionIdentifier_ = "ImGui.FileDialog.CreateFolder.Caption";
        private const string CreateFolderTextIdentifier_ = "ImGui.FileDialog.CreateFolder.Text";

        public static string Ok() => Localize(OkIdentifier_, Ok_);
        public static string Cancel() => Localize(CancelIdentifier_, Cancel_);
        public static string Yes() => Localize(YesIdentifier_, Yes_);
        public static string No() => Localize(NoIdentifier_, No_);
        public static string Save() => Localize(SaveIdentifier_, Save_);
        public static string Search() => Localize(SearchIdentifier_, SearchPlaceholder_);
        public static string ItemName() => Localize(ItemNameIdentifier_, ItemName_);
        public static string ItemType() => Localize(ItemTypeIdentifier_, ItemType_);
        public static string ItemDateModified() => Localize(ItemDateModifiedIdentifier_, ItemDateModified_);
        public static string SelectedFile() => Localize(SelectedFileIdentifier_, SelectedFile_);
        public static string ReplaceFileCaption() => Localize(ReplaceFileCaptionIdentifier_, ReplaceFileCaption_);
        public static string ReplaceFileText(string path) => Localize(ReplaceFileTextIdentifier_, ReplaceFileText_, path);
        public static string CreateFolderCaption() => Localize(CreateFolderCaptionIdentifier_, CreateFolderCaption_);
        public static string CreateFolderText() => Localize(CreateFolderTextIdentifier_, CreateFolderText_);

        private static string Localize(string localizationId, string fallback, params object[] args)
        {
            ILocalizer localizer = Application.Instance.Localizer;
            var text = string.Empty;

            if (!localizer?.TryLocalize(localizationId, out text, args) ?? true)
                text = string.Format(fallback, args);

            return text;
        }
    }
}
