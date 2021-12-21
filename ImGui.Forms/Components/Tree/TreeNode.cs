using System.Collections.Generic;
using ImGui.Forms.Components.Layouts;

namespace ImGui.Forms.Components.Tree
{
    public class TreeNode<TNodeData>
    {
        private TreeView<TNodeData> _parentView;
        private readonly ObservableList<TreeNode<TNodeData>> _nodes;

        private bool _isExpanded;

        public string Caption { get; set; } = string.Empty;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                var hasChanged = _isExpanded != value;
                _isExpanded = value;

                if (hasChanged)
                    OnExpandedChanged();
            }
        }

        public IList<TreeNode<TNodeData>> Nodes => _nodes;

        public TreeNode<TNodeData> Parent { get; private set; }

        public TNodeData Data { get; set; }

        public TreeNode()
        {
            _nodes = new ObservableList<TreeNode<TNodeData>>();
            _nodes.ItemAdded += _nodes_ItemAdded;
            _nodes.ItemRemoved += _nodes_ItemRemoved;
        }

        internal static TreeNode<TNodeData> Create(TreeView<TNodeData> parent)
        {
            return new TreeNode<TNodeData> { _parentView = parent };
        }

        private void _nodes_ItemAdded(object sender, ItemEventArgs<TreeNode<TNodeData>> e)
        {
            e.Item.Parent = this;
            SetParents(e.Item, _parentView);
        }

        private void _nodes_ItemRemoved(object sender, ItemEventArgs<TreeNode<TNodeData>> e)
        {
            e.Item.Parent = null;
            SetParents(e.Item, null);
        }

        private void SetParents(TreeNode<TNodeData> input, TreeView<TNodeData> parent)
        {
            input._parentView = parent;

            foreach (var node in input.Nodes)
                SetParents(node, parent);
        }

        private void OnExpandedChanged()
        {
            if (_isExpanded)
                _parentView?.OnNodeExpanded(this);
            else
                _parentView?.OnNodeCollapsed(this);
        }
    }
}
