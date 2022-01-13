using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Controls.Tree;
using ImGui.Forms.Models;
using ImGui.Forms.Support;
using ImGuiNET;

namespace ImGui.Forms.Modals.IO
{
    public class SaveFileDialog : Modal
    {
        private History<string> _dictHistory;
        private string _currentDir;

        private readonly ArrowButton _backBtn;
        private readonly ArrowButton _forBtn;
        private readonly TextBox _dirTextBox;
        private readonly TextBox _searchTextBox;
        private readonly TextBox _selectedFileTextBox;

        private readonly TreeView<string> _treeView;
        private readonly DataTable<FileEntry> _fileTable;

        private readonly Button _saveBtn;

        public string InitialDirectory { get; set; }

        public string SelectedPath { get; private set; }

        public SaveFileDialog(string filePath = null)
        {
            #region Controls

            _backBtn = new ArrowButton { Direction = ImGuiDir.Left, Enabled = false };
            _forBtn = new ArrowButton { Direction = ImGuiDir.Right, Enabled = false };
            _dirTextBox = new TextBox { Width = .7f };
            _searchTextBox = new TextBox { Width = .3f, Placeholder = "Search..." };
            _selectedFileTextBox = new TextBox { Width = .7f };

            _treeView = new TreeView<string> { Size = new Size(.3f, 1f) };
            _treeView.NodeExpanded += _treeView_NodeExpanded;

            _fileTable = new DataTable<FileEntry> { Size = new Size(.7f, 1f) };
            _fileTable.Columns.Add(new DataTableColumn<FileEntry>(x => x.Name, "Name"));
            _fileTable.Columns.Add(new DataTableColumn<FileEntry>(x => x.Type, "Type"));
            _fileTable.Columns.Add(new DataTableColumn<FileEntry>(x => x.DateModified.ToString(CultureInfo.CurrentCulture), "Date modified"));

            var cnlBtn = new Button { Caption = "Cancel", Width = 80 };
            _saveBtn = new Button { Caption = "Save", Width = 80, Enabled = !string.IsNullOrEmpty(filePath) };

            #endregion

            if (!string.IsNullOrEmpty(filePath))
            {
                InitialDirectory = Path.GetDirectoryName(filePath);
                SelectedPath = filePath;

                _selectedFileTextBox.Text = Path.GetFileName(filePath);
            }

            #region Events

            _backBtn.Clicked += _backBtn_Clicked;
            _forBtn.Clicked += _forBtn_Clicked;
            _dirTextBox.FocusLost += _dirTextBox_FocusLost;
            _searchTextBox.TextChanged += _searchTextBox_TextChanged;
            _selectedFileTextBox.TextChanged += _selectedFileTextBox_TextChanged;

            _treeView.SelectedNodeChanged += _treeView_SelectedNodeChanged;
            _fileTable.DoubleClicked += _fileTable_DoubleClicked;
            _fileTable.SelectedRowsChanged += _fileTable_SelectedRowsChanged;

            cnlBtn.Clicked += CnlBtn_Clicked;
            _saveBtn.Clicked += SaveBtnClicked;

            #endregion

            var width = (int)Math.Ceiling(Application.Instance.MainForm.Width * .9f);
            var height = (int)Math.Ceiling(Application.Instance.MainForm.Height * .8f);
            Size = new Vector2(width, height);

            Content = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 5,
                Items =
                {
                    // Top bar with arrow buttons, directory text box, and search bar
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        Size = new Size(1f, -1),
                        ItemSpacing = 5,
                        Items =
                        {
                            _backBtn,
                            _forBtn,
                            _dirTextBox,
                            _searchTextBox
                        }
                    },

                    // File tree and file view table
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

                    // Selected file name
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Size = new Size(7f, -1),
                        ItemSpacing = 5,
                        Items =
                        {
                            new Label {Caption = "File name:"},
                            _selectedFileTextBox
                        }
                    },

                    // Dialog buttons
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Size = new Size(1f, -1),
                        ItemSpacing = 5,
                        Items =
                        {
                            new StackItem(_saveBtn),
                            new StackItem(cnlBtn)
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

            // Initialize file tree and file view
            var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _treeView.Nodes.Add(new TreeNode<string> { Caption = "Desktop", Data = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Nodes = { new TreeNode<string>() } });
            _treeView.Nodes.Add(new TreeNode<string> { Caption = Path.GetFileName(userDir), Data = userDir, Nodes = { new TreeNode<string>() } });

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
            { Name = Path.GetFileName(x), Type = "D", DateModified = Directory.GetLastAccessTime(x) });
        }

        private IEnumerable<FileEntry> GetFiles(string dir)
        {
            // Get files in directory
            var searchTerm = string.IsNullOrEmpty(_searchTextBox.Text) ? "*" : _searchTextBox.Text;
            var files = Directory.EnumerateFiles(dir, searchTerm);

            return files.Select(x => new FileEntry { Name = Path.GetFileName(x), Type = "F", DateModified = File.GetLastWriteTime(x) });
        }

        private async Task<bool> ShouldOverwrite(string path)
        {
            if (!File.Exists(path))
                return true;

            return await MessageBox.ShowYesNoAsync("File exists", $"Do you want to overwrite file {Path.GetFileName(path)}?") != DialogResult.No;
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

        #region File Table

        private void _fileTable_SelectedRowsChanged(object sender, EventArgs e)
        {
            if (!_fileTable.SelectedRows.Any())
                return;

            _selectedFileTextBox.Text = _fileTable.SelectedRows.First().Data.Name;
        }

        private async void _fileTable_DoubleClicked(object sender, EventArgs e)
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

            if (!await ShouldOverwrite(SelectedPath))
                return;

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
                    node.Nodes.Add(new TreeNode<string> { Caption = dirName, Data = dirName, Nodes = { new TreeNode<string>() } });
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

        private async void SaveBtnClicked(object sender, EventArgs e)
        {
            // TODO: Rethink usage and setting of SelectedPath throughout control
            SelectedPath = Path.Combine(_currentDir, _selectedFileTextBox.Text);

            if (!await ShouldOverwrite(SelectedPath))
                return;

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
            SelectedPath = Path.Combine(_currentDir, _selectedFileTextBox.Text);

            _saveBtn.Enabled = !string.IsNullOrEmpty(_selectedFileTextBox.Text);
        }

        private void _searchTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateFileView();
        }

        #endregion

        #endregion

        private class FileEntry
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public DateTime DateModified { get; set; }
        }
    }
}
