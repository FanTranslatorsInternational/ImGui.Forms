using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Support;

namespace ImGui.Forms.Controls.Tree;

public class TreeView<TNodeData> : Component
{
    private readonly TreeNode<TNodeData> _rootNode;
    private TreeNode<TNodeData> _selectedNode;

    #region Properties

    public Models.Size Size { get; set; } = Models.Size.Parent;

    public IList<TreeNode<TNodeData>> Nodes => _rootNode.Nodes;

    public TreeNode<TNodeData> SelectedNode
    {
        get => _selectedNode;
        set
        {
            var invokeChanged = _selectedNode != value;

            _selectedNode = value;
            if (invokeChanged)
                OnSelectedNodeChanged();
        }
    }

    public ContextMenu ContextMenu { get; set; }

    #endregion

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

        if (Hexa.NET.ImGui.ImGui.BeginChild($"{Id}", new Vector2(contentRect.Width, contentRect.Height), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
        {
            UpdateNodes(Nodes);
        }

        Hexa.NET.ImGui.ImGui.EndChild();
    }

    private void UpdateNodes(IList<TreeNode<TNodeData>> nodes)
    {
        foreach (var node in nodes.ToArray())
        {
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (Enabled) flags |= ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.OpenOnArrow;
            if (node.Nodes.Count <= 0) flags |= ImGuiTreeNodeFlags.Leaf;
            if (SelectedNode == node) flags |= ImGuiTreeNodeFlags.Selected;

            // Add node
            int nodeId = Application.Instance.Ids!.Get(node);

            Hexa.NET.ImGui.ImGui.PushID(nodeId);
            Hexa.NET.ImGui.ImGui.SetNextItemOpen(node.IsExpanded);

            if (!node.TextColor.IsEmpty)
                Hexa.NET.ImGui.ImGui.PushStyleColor(ImGuiCol.Text, node.TextColor.ToUInt32());

            ImFontPtr? nodeFontPtr = node.Font?.GetPointer();
            if (nodeFontPtr != null)
                Hexa.NET.ImGui.ImGui.PushFont(nodeFontPtr.Value, node.Font!.Data.Size);

            Hexa.NET.ImGui.ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            Hexa.NET.ImGui.ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 2));

            bool expanded = Hexa.NET.ImGui.ImGui.TreeNodeEx(node.Text, flags);
            if (Hexa.NET.ImGui.ImGui.IsItemFocused())
                SelectedNode = node;

            Hexa.NET.ImGui.ImGui.PopStyleVar(2);

            bool changedExpansion = expanded != node.IsExpanded;
            if (changedExpansion && Enabled)
                node.IsExpanded = expanded;

            if (nodeFontPtr != null)
                Hexa.NET.ImGui.ImGui.PopFont();

            if (!node.TextColor.IsEmpty)
                Hexa.NET.ImGui.ImGui.PopStyleColor();

            Hexa.NET.ImGui.ImGui.PopID();

            // Change selected node, if expansion of node did not change, and mouse is over node
            if (!changedExpansion && IsTreeNodeClicked() && SelectedNode != node && Enabled)
                SelectedNode = node;

            // Add context, only if mouse is over a tree node
            if (SelectedNode == node)
                ContextMenu?.Update();

            // Add children nodes, if parent is expanded
            if (node is { IsExpanded: true, Nodes.Count: > 0 })
                UpdateNodes(node.Nodes);

            if (expanded)
                Hexa.NET.ImGui.ImGui.TreePop();
        }
    }

    private bool IsTreeNodeClicked()
    {
        return (Hexa.NET.ImGui.ImGui.IsMouseClicked(ImGuiMouseButton.Right) || Hexa.NET.ImGui.ImGui.IsMouseClicked(ImGuiMouseButton.Left)) && Hexa.NET.ImGui.ImGui.IsItemHovered();
    }

    private void OnSelectedNodeChanged()
    {
        SelectedNodeChanged?.Invoke(this, EventArgs.Empty);
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