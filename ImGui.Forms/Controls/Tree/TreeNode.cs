﻿using System.Collections.Generic;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Localization;
using ImGui.Forms.Resources;

namespace ImGui.Forms.Controls.Tree
{
    public abstract class TreeNode
    {
        private bool _isExpanded;

        public LocalizedString Text { get; set; } = string.Empty;

        public ThemedColor TextColor { get; set; }

        public FontResource Font { get; set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                bool hasChanged = _isExpanded != value;
                _isExpanded = value;

                if (hasChanged)
                    OnExpandedChanged();
            }
        }

        public abstract IEnumerable<TreeNode> EnumerateNodes();

        protected abstract void OnExpandedChanged();
    }

    public class TreeNode<TNodeData> : TreeNode
    {
        private readonly ObservableList<TreeNode<TNodeData>> _nodes;

        private TreeView<TNodeData> _parentView;
        private bool _isRoot;

        public bool IsRoot => Parent?._isRoot ?? true;

        public IList<TreeNode<TNodeData>> Nodes => _nodes;

        public TreeNode<TNodeData> Parent { get; private set; }

        public TNodeData Data { get; set; }

        public TreeNode()
        {
            _nodes = new ObservableList<TreeNode<TNodeData>>();
            _nodes.ItemAdded += _nodes_ItemAdded;
            _nodes.ItemRemoved += _nodes_ItemRemoved;
            _nodes.ItemSet += _nodes_ItemSet;
            _nodes.ItemInserted += _nodes_ItemInserted;
        }

        public void Remove()
        {
            Parent.Nodes.Remove(this);
        }

        internal static TreeNode<TNodeData> Create(TreeView<TNodeData> parent)
        {
            return new TreeNode<TNodeData> { _parentView = parent, _isRoot = true };
        }

        private void _nodes_ItemAdded(object sender, ItemEventArgs<TreeNode<TNodeData>> e)
        {
            e.Item.Parent = this;
            SetParents(e.Item, _parentView);
        }

        private void _nodes_ItemRemoved(object sender, ItemEventArgs<TreeNode<TNodeData>> e)
        {
            if (_parentView.SelectedNode == e.Item)
                _parentView.SelectedNode = null;

            e.Item.Parent = null;
            SetParents(e.Item, null);
        }

        private void _nodes_ItemInserted(object sender, ItemEventArgs<TreeNode<TNodeData>> e)
        {
            e.Item.Parent = this;
            SetParents(e.Item, _parentView);
        }

        private void _nodes_ItemSet(object sender, ItemEventArgs<TreeNode<TNodeData>> e)
        {
            e.Item.Parent = this;
            SetParents(e.Item, _parentView);
        }

        private void SetParents(TreeNode<TNodeData> input, TreeView<TNodeData> parent)
        {
            input._parentView = parent;

            foreach (var node in input.Nodes)
            {
                if (_parentView != null && _parentView.SelectedNode == node)
                    _parentView.SelectedNode = null;

                SetParents(node, parent);
            }
        }

        public override IEnumerable<TreeNode> EnumerateNodes()
        {
            return _nodes;
        }

        protected override void OnExpandedChanged()
        {
            if (IsExpanded)
                _parentView?.OnNodeExpanded(this);
            else
                _parentView?.OnNodeCollapsed(this);
        }
    }
}
