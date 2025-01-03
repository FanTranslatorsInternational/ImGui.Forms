﻿using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Tree;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;

namespace ImGui.Forms.Modals.IO
{
    public class SelectFolderDialog : Modal
    {
        private TreeView<string> _treeView;

        private Button _newFolderButton;
        private Button _okButton;
        private Button _cancelButton;

        public string Directory { get; set; }

        public SelectFolderDialog()
        {
            #region Controls

            _treeView = new TreeView<string>();

            _newFolderButton = new Button { Text = LocalizationResources.CreateFolderCaption(), Enabled = false, Padding = new Vector2(5, 2) };
            _okButton = new Button { Text = LocalizationResources.Ok(), Width = 80, Enabled = false };
            _cancelButton = new Button { Text = LocalizationResources.Cancel(), Width = 80 };

            #endregion

            #region Events

            _treeView.SelectedNodeChanged += _treeView_SelectedNodeChanged;
            _treeView.NodeExpanded += _treeView_NodeExpanded;

            _newFolderButton.Clicked += _newFolderButton_Clicked;
            _okButton.Clicked += _okButton_Clicked;
            _cancelButton.Clicked += _cancelButton_Clicked;

            #endregion

            #region Main content

            Size = new Size(SizeValue.Relative(.9f), SizeValue.Relative(.8f));

            Result = DialogResult.Cancel;
            Content = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Items =
                {
                    _treeView,
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        ItemSpacing = 4,
                        Size = new Size(1f,SizeValue.Content),
                        Items =
                        {
                            _newFolderButton
                        }
                    },
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        ItemSpacing = 4,
                        Size = new Size(SizeValue.Parent,SizeValue.Content),
                        Items =
                        {
                            _okButton,
                            _cancelButton
                        }
                    }
                }
            };

            #endregion
        }

        protected override void ShowInternal()
        {
            Directory = GetInitialDirectory();

            // Initialize file tree and file view
            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            _treeView.Nodes.Add(new TreeNode<string> { Text = Path.GetFileName(desktopDir), Data = desktopDir, Nodes = { new TreeNode<string>() } });
            _treeView.Nodes.Add(new TreeNode<string> { Text = Path.GetFileName(userDir), Data = userDir, Nodes = { new TreeNode<string>() } });

            foreach (var drive in DriveInfo.GetDrives())
                _treeView.Nodes.Add(new TreeNode<string> { Text = drive.Name, Data = drive.RootDirectory.Name, Nodes = { new TreeNode<string>() } });
        }

        #region Events

        private async void _newFolderButton_Clicked(object sender, EventArgs e)
        {
            var path = GetNodePath(_treeView.SelectedNode);
            var newFolderName = await InputBox.ShowAsync(LocalizationResources.CreateFolderCaption(), LocalizationResources.CreateFolderText());

            if (string.IsNullOrEmpty(newFolderName))
                return;

            // Create directory, if not exists
            var newDir = Path.Combine(path, newFolderName);
            if (System.IO.Directory.Exists(newDir))
                return;

            System.IO.Directory.CreateDirectory(newDir);
            _treeView.SelectedNode.Nodes.Add(new TreeNode<string> { Text = newFolderName, Data = Path.Combine(path, newFolderName) });
        }

        private void _okButton_Clicked(object sender, EventArgs e)
        {
            Result = DialogResult.Ok;
            Close();
        }

        private void _cancelButton_Clicked(object sender, EventArgs e)
        {
            Result = DialogResult.Cancel;
            Close();
        }

        private void _treeView_SelectedNodeChanged(object sender, EventArgs e)
        {
            var node = _treeView.SelectedNode;
            Directory = GetNodePath(node);

            var dirExists = System.IO.Directory.Exists(Directory);

            // Button enablement
            _newFolderButton.Enabled = dirExists;
            _okButton.Enabled = dirExists;
        }

        private void _treeView_NodeExpanded(object sender, NodeEventArgs<string> e)
        {
            var node = e.Node;

            // Remove old nodes, if directory no longer exists
            foreach (var currentNode in node.Nodes.ToArray())
            {
                // HINT: This line is also used to remove the dud node, that is placed to make the tree view arrow visible; Necessary to achieve lazy loading in a tree
                if (!System.IO.Directory.Exists(GetNodePath(currentNode)) || string.IsNullOrEmpty(currentNode.Data))
                    node.Nodes.Remove(currentNode);
            }

            // Add new nodes, if new directories are encountered
            var nodePath = GetNodePath(node);
            foreach (var dirName in System.IO.Directory.EnumerateDirectories(nodePath).Select(Path.GetFileName))
            {
                var existingNode = node.Nodes.FirstOrDefault(x => x.Data == dirName);
                if (existingNode == null)
                    node.Nodes.Add(new TreeNode<string> { Text = dirName, Data = dirName, Nodes = { new TreeNode<string>() } });
            }
        }

        #endregion

        #region Support methods

        private string GetInitialDirectory()
        {
            if (!string.IsNullOrEmpty(Directory) && System.IO.Directory.Exists(Directory))
                return Directory;

            return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        private string GetNodePath(TreeNode<string> node)
        {
            if (node.Parent != null)
                return Path.Combine(GetNodePath(node.Parent), node.Data ?? string.Empty);

            return node.Data ?? string.Empty;
        }

        #endregion
    }
}
