﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Controls.Tree;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGui.Forms.Support;
using ImGuiNET;

namespace ImGui.Forms.Modals.IO
{
    public class OpenFileDialog : Modal
    {
        private const string ItemTypeDirectory_ = "D";
        private const string ItemTypeFile_ = "F";

        private History<string> _dictHistory;
        private string _currentDir;

        private readonly ArrowButton _backBtn;
        private readonly ArrowButton _forBtn;
        private readonly TextBox _dirTextBox;
        private readonly TextBox _searchTextBox;
        private readonly TextBox _selectedFileTextBox;
        private readonly ComboBox<FileFilter> _fileFilters;

        private readonly TreeView<string> _treeView;
        private readonly DataTable<FileEntry> _fileTable;

        private readonly Button _openBtn;

        public string InitialDirectory { get; set; }
        public string InitialFileName { get; set; }

        public IList<FileFilter> FileFilters { get; }

        public string SelectedPath { get; private set; }

        public OpenFileDialog()
        {
            var filters = new ObservableList<FileFilter>();
            filters.ItemAdded += Filters_ItemAdded;
            filters.ItemRemoved += Filters_ItemRemoved;
            filters.ItemSet += Filters_ItemSet;
            filters.ItemInserted += Filters_ItemInserted;

            FileFilters = filters;

            #region Controls

            _backBtn = new ArrowButton { Direction = ImGuiDir.Left, Enabled = false };
            _forBtn = new ArrowButton { Direction = ImGuiDir.Right, Enabled = false };
            _dirTextBox = new TextBox { Width = .7f };
            _searchTextBox = new TextBox { Width = .3f, Placeholder = LocalizationResources.Search() };
            _selectedFileTextBox = new TextBox { Width = 1f };
            _fileFilters = new ComboBox<FileFilter>();

            _treeView = new TreeView<string> { Size = new Size(.3f, 1f) };

            _fileTable = new DataTable<FileEntry> { Size = new Size(.7f, 1f) };
            _fileTable.Columns.Add(new DataTableColumn<FileEntry>(x => x.Name, LocalizationResources.ItemName()));
            _fileTable.Columns.Add(new DataTableColumn<FileEntry>(x => x.Type, LocalizationResources.ItemType()));
            _fileTable.Columns.Add(new DataTableColumn<FileEntry>(x => x.DateModified.ToString(CultureInfo.CurrentCulture), LocalizationResources.ItemDateModified()));

            var cancelBtn = new Button { Text = LocalizationResources.Cancel(), Width = 80 };
            _openBtn = new Button { Text = LocalizationResources.Ok(), Width = 80, Enabled = false };

            #endregion

            #region Events

            _backBtn.Clicked += _backBtn_Clicked;
            _forBtn.Clicked += _forBtn_Clicked;
            _dirTextBox.FocusLost += _dirTextBox_FocusLost;
            _searchTextBox.TextChanged += _searchTextBox_TextChanged;
            _selectedFileTextBox.TextChanged += _selectedFileTextBox_TextChanged;

            _treeView.SelectedNodeChanged += _treeView_SelectedNodeChanged;
            _treeView.NodeExpanded += _treeView_NodeExpanded;

            _fileTable.DoubleClicked += _fileTable_DoubleClicked;
            _fileTable.SelectedRowsChanged += _fileTable_SelectedRowsChanged;

            _fileFilters.SelectedItemChanged += _fileFilters_SelectedItemChanged;

            cancelBtn.Clicked += CnlBtn_Clicked;
            _openBtn.Clicked += OpenBtnClicked;

            #endregion

            Result = DialogResult.Cancel;

            Size = new Size(SizeValue.Relative(.9f), SizeValue.Relative(.8f));

            Content = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 5,
                Items =
                {
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        Size = Size.WidthAlign,
                        ItemSpacing = 5,
                        Items =
                        {
                            _backBtn,
                            _forBtn,
                            _dirTextBox,
                            _searchTextBox
                        }
                    },

                    new StackItem(new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        ItemSpacing = 5,
                        Items =
                        {
                            _treeView,
                            _fileTable
                        }
                    }) {VerticalAlignment = VerticalAlignment.Top},

                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        Size = Size.WidthAlign,
                        ItemSpacing = 5,
                        Items =
                        {
                            new Label {Text = LocalizationResources.SelectedFile()},
                            _selectedFileTextBox,
                            _fileFilters
                        }
                    },

                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Size = Size.WidthAlign,
                        ItemSpacing = 5,
                        Items =
                        {
                            _openBtn,
                            cancelBtn
                        }
                    }
                }
            };
        }

        protected override void ShowInternal()
        {
            // Initialize fields
            _currentDir = GetInitialDirectory();
            _dictHistory = new History<string>(_currentDir);

            _dirTextBox.Text = _currentDir;
            _selectedFileTextBox.Text = InitialFileName;

            // Initialize file tree and file view
            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            _treeView.Nodes.Add(new TreeNode<string> { Text = Path.GetFileName(desktopDir), Data = desktopDir, Nodes = { new TreeNode<string>() } });
            _treeView.Nodes.Add(new TreeNode<string> { Text = Path.GetFileName(userDir), Data = userDir, Nodes = { new TreeNode<string>() } });

            UpdateFileView();
        }

        #region Support

        private string GetInitialDirectory()
        {
            if (!string.IsNullOrEmpty(InitialDirectory) && Directory.Exists(InitialDirectory))
                return InitialDirectory;

            return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        private string GetNodePath(TreeNode<string> node)
        {
            if (node.Parent != null)
                return Path.Combine(GetNodePath(node.Parent), node.Data ?? string.Empty);

            return node.Data ?? string.Empty;
        }

        private IEnumerable<FileEntry> GetDirectories(string dir)
        {
            return Directory.EnumerateDirectories(dir).Select(x => new FileEntry
            { Name = Path.GetFileName(x), Type = ItemTypeDirectory_, DateModified = Directory.GetLastAccessTime(x) });
        }

        private IEnumerable<FileEntry> GetFiles(string dir)
        {
            var useSearch = !string.IsNullOrEmpty(_searchTextBox.Text);
            var searchTerm = string.IsNullOrEmpty(_searchTextBox.Text) ? "*" : _searchTextBox.Text;

            // Get files in directory
            var files = Directory.EnumerateFiles(dir, searchTerm);

            // Apply extension filter, if set
            if (!useSearch && _fileFilters.SelectedItem != null)
            {
                IList<string> extensions = _fileFilters.SelectedItem.Content.Extensions;
                if (extensions.All(e => e != "*"))
                    files = files.Where(f => GetFileExtensions(f).Any(e => extensions.Contains(e)));
            }

            return files.Select(x => new FileEntry { Name = Path.GetFileName(x), Type = ItemTypeFile_, DateModified = File.GetLastWriteTime(x) });
        }

        private string[] GetFileExtensions(string filePath)
        {
            string fileName = Path.GetFileName(filePath);

            string[] fileNameParts = fileName.Split('.');
            if (fileNameParts.Length - 1 < 0)
                return Array.Empty<string>();

            var result = new string[fileNameParts.Length - 1];
            for (var i = 1; i < fileNameParts.Length; i++)
            {
                string extension = fileNameParts[i];
                if (i - 1 > 0)
                    extension = result[i - 2] + '.' + extension;

                result[i - 1] = extension;
            }

            return result;
        }

        #endregion

        #region Updates

        private void UpdateFileView()
        {
            _fileTable.Rows = GetDirectories(_currentDir).Concat(GetFiles(_currentDir)).Select(fe => new DataTableRow<FileEntry>(fe)).ToArray();
        }

        private void UpdateButtonEnablement()
        {
            _backBtn.Enabled = !_dictHistory.IsFirstItem();
            _forBtn.Enabled = !_dictHistory.IsLastItem();
        }

        #endregion

        #region Events

        #region File Filters

        private void Filters_ItemAdded(object sender, ItemEventArgs<FileFilter> e)
        {
            _fileFilters.Items.Add(new DropDownItem<FileFilter>(e.Item));
        }

        private void Filters_ItemRemoved(object sender, ItemEventArgs<FileFilter> e)
        {
            _fileFilters.Items.Remove(e.Item);
        }

        private void Filters_ItemInserted(object sender, ItemEventArgs<FileFilter> e)
        {
            _fileFilters.Items.Insert(e.Index, new DropDownItem<FileFilter>(e.Item));
        }

        private void Filters_ItemSet(object sender, ItemEventArgs<FileFilter> e)
        {
            _fileFilters.Items[e.Index] = new DropDownItem<FileFilter>(e.Item);
        }

        private void _fileFilters_SelectedItemChanged(object sender, EventArgs e)
        {
            UpdateFileView();
        }

        #endregion

        #region File Table

        private void _fileTable_SelectedRowsChanged(object sender, EventArgs e)
        {
            if (!_fileTable.SelectedRows.Any())
                return;

            _selectedFileTextBox.Text = _fileTable.SelectedRows.First().Data.Name;
        }

        private void _fileTable_DoubleClicked(object sender, EventArgs e)
        {
            if (!_fileTable.SelectedRows.Any())
                return;

            if (Directory.Exists(SelectedPath))
            {
                // Set current directory
                _currentDir = _dirTextBox.Text = SelectedPath;

                // Push given directory to history
                _dictHistory.PushItem(SelectedPath);

                // Update file view
                UpdateFileView();

                // Update button enablement
                UpdateButtonEnablement();

                return;
            }

            Result = DialogResult.Ok;
            Close();
        }

        #endregion

        #region Tree View

        private void _treeView_SelectedNodeChanged(object sender, EventArgs e)
        {
            var node = _treeView.SelectedNode;
            var path = GetNodePath(node);

            if (_currentDir == path || !Directory.Exists(path))
                return;

            // Set current directory
            _currentDir = _dirTextBox.Text = path;

            // Push given directory to history
            _dictHistory.PushItem(path);

            // Update file view
            UpdateFileView();

            // Update button enablement
            UpdateButtonEnablement();
        }

        private void _treeView_NodeExpanded(object sender, NodeEventArgs<string> e)
        {
            var node = e.Node;

            // Remove old nodes, if directory no longer exists
            foreach (var currentNode in node.Nodes.ToArray())
            {
                // HINT: This line is also used to remove the dud node, that is placed to make the tree view arrow visible; Necessary to achieve lazy loading in a tree
                if (!Directory.Exists(GetNodePath(currentNode)) || string.IsNullOrEmpty(currentNode.Data))
                    node.Nodes.Remove(currentNode);
            }

            // Add new nodes, if new directories are encountered
            var nodePath = GetNodePath(node);
            foreach (var dirName in Directory.EnumerateDirectories(nodePath).Select(Path.GetFileName))
            {
                var existingNode = node.Nodes.FirstOrDefault(x => x.Data == dirName);
                if (existingNode == null)
                    node.Nodes.Add(new TreeNode<string> { Text = dirName, Data = dirName, Nodes = { new TreeNode<string>() } });
            }
        }

        #endregion

        #region Buttons

        private void _backBtn_Clicked(object sender, EventArgs e)
        {
            // Get previous path
            _dictHistory.MoveBackward();
            _currentDir = _dictHistory.GetCurrentItem();

            // Update path text box
            _dirTextBox.Text = _currentDir;

            // Update file view
            UpdateFileView();

            // Update button enablement
            UpdateButtonEnablement();
        }

        private void _forBtn_Clicked(object sender, EventArgs e)
        {
            // Get next path
            _dictHistory.MoveForward();
            _currentDir = _dictHistory.GetCurrentItem();

            // Update path text box
            _dirTextBox.Text = _currentDir;

            // Update file view
            UpdateFileView();

            // Update button enablement
            UpdateButtonEnablement();
        }

        private void OpenBtnClicked(object sender, EventArgs e)
        {
            Result = DialogResult.Ok;
            Close();
        }

        private void CnlBtn_Clicked(object sender, EventArgs e)
        {
            SelectedPath = null;

            Result = DialogResult.Cancel;
            Close();
        }

        #endregion

        #region Textboxes

        private void _dirTextBox_FocusLost(object sender, EventArgs e)
        {
            if (_currentDir == _dirTextBox.Text)
                return;

            // Check if given directory is valid
            if (!Directory.Exists(_dirTextBox.Text))
            {
                // Restore current directory, if given directory is invalid
                _dirTextBox.Text = _currentDir;
                return;
            }

            // Push given directory to history
            _dictHistory.PushItem(_dirTextBox.Text);

            // Update current directory
            _currentDir = _dirTextBox.Text;

            // Update file view
            UpdateFileView();

            // Update button enablement
            UpdateButtonEnablement();
        }

        private void _selectedFileTextBox_TextChanged(object sender, EventArgs e)
        {
            var fullPath = Path.Combine(_currentDir, _selectedFileTextBox.Text);

            _openBtn.Enabled = !string.IsNullOrEmpty(_selectedFileTextBox.Text) && File.Exists(fullPath);

            SelectedPath = fullPath;
        }

        private void _searchTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateFileView();
        }

        #endregion

        #endregion

        class FileEntry
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public DateTime DateModified { get; set; }
        }
    }
}
