using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Extensions;
using ImGuiNET;
using Rectangle = Veldrid.Rectangle;

namespace ImGui.Forms.Controls.Tree
{
    public class TreeView<TNodeData> : Component
    {
        private readonly TreeNode<TNodeData> _rootNode;
        private TreeNode<TNodeData> _selectedNode;

        public Models.Size Size { get; set; } = Models.Size.Parent;

        public IList<TreeNode<TNodeData>> Nodes => _rootNode.Nodes;

        public TreeNode<TNodeData> SelectedNode
        {
            get => _selectedNode;
            set
            {
                _selectedNode = value;
                OnSelectedNodeChanged();
            }
        }

        public ContextMenu ContextMenu { get; set; }

        #region Events

        public event EventHandler SelectedNodeChanged;
        public event EventHandler<NodeEventArgs<TNodeData>> NodeExpanded;
        public event EventHandler<NodeEventArgs<TNodeData>> NodeCollapsed;

        #endregion

        public TreeView()
        {
            _rootNode = TreeNode<TNodeData>.Create(this);
        }

        public override Models.Size GetSize()
        {
            return Size;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            if (Nodes.Count <= 0)
                return;

            var anyNodeHovered = false;
            if (ImGuiNET.ImGui.BeginChild($"{Id}", new Vector2(contentRect.Width, contentRect.Height), false, ImGuiWindowFlags.HorizontalScrollbar))
            {
                UpdateNodes(Nodes, ref anyNodeHovered);
            }

            ImGuiNET.ImGui.EndChild();
        }

        private void UpdateNodes(IList<TreeNode<TNodeData>> nodes, ref bool nodeHovered)
        {
            foreach (var node in nodes.ToArray())
            {
                var flags = ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.OpenOnArrow;
                if (node.Nodes.Count <= 0) flags |= ImGuiTreeNodeFlags.Leaf;
                if (SelectedNode == node) flags |= ImGuiTreeNodeFlags.Selected;

                // Add node
                var nodeId = Application.Instance.IdFactory.Get(node);

                ImGuiNET.ImGui.PushID(nodeId);
                ImGuiNET.ImGui.SetNextItemOpen(node.IsExpanded);

                if (node.TextColor != Color.Empty)
                    ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Text, node.TextColor.ToUInt32());

                if (node.Font != null)
                    ImGuiNET.ImGui.PushFont((ImFontPtr)node.Font);

                var expanded = ImGuiNET.ImGui.TreeNodeEx(node.Caption ?? string.Empty, flags);
                var changedExpansion = expanded != node.IsExpanded;
                if (changedExpansion)
                    node.IsExpanded = expanded;

                if (node.Font != null)
                    ImGuiNET.ImGui.PopFont();

                if (node.TextColor != Color.Empty)
                    ImGuiNET.ImGui.PopStyleColor();

                ImGuiNET.ImGui.PopID();

                // Check if tree node is hovered
                nodeHovered |= ImGuiNET.ImGui.IsItemHovered();

                // Change selected node, if expansion of node did not change, and mouse is over node
                if (!changedExpansion && IsTreeNodeClicked() && SelectedNode != node)
                    SelectedNode = node;

                // Add context, only if mouse is over a tree node
                if (SelectedNode == node)
                    ContextMenu?.Update();

                // Add children nodes, if parent is expanded
                if (!node.IsExpanded)
                    continue;

                if (node.Nodes.Count > 0)
                    UpdateNodes(node.Nodes, ref nodeHovered);

                ImGuiNET.ImGui.TreePop();
            }
        }

        private bool IsTreeNodeClicked()
        {
            return (ImGuiNET.ImGui.IsMouseClicked(ImGuiMouseButton.Right) || ImGuiNET.ImGui.IsMouseClicked(ImGuiMouseButton.Left)) && ImGuiNET.ImGui.IsItemHovered();
        }

        private void OnSelectedNodeChanged()
        {
            SelectedNodeChanged?.Invoke(this, new EventArgs());
        }

        internal void OnNodeExpanded(TreeNode<TNodeData> node)
        {
            NodeExpanded?.Invoke(this, new NodeEventArgs<TNodeData>(node));
        }

        internal void OnNodeCollapsed(TreeNode<TNodeData> node)
        {
            NodeCollapsed?.Invoke(this, new NodeEventArgs<TNodeData>(node));
        }
    }

    public class NodeEventArgs<TNodeData> : EventArgs
    {
        public TreeNode<TNodeData> Node { get; }

        public NodeEventArgs(TreeNode<TNodeData> node)
        {
            Node = node;
        }
    }
}
