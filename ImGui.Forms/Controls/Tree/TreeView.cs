using Hexa.NET.ImGui;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Extensions;
using ImGui.Forms.Models.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms.Support;

namespace ImGui.Forms.Controls.Tree;

public class TreeView<TNodeData> : Component
{
    private const int FirstArrowSelectionDelta_ = 30;
    private const int ArrowSelectionFrameDelta_ = 5;

    private static readonly KeyCommand PreviousNodeKey = new(ImGuiKey.UpArrow);
    private static readonly KeyCommand NextNodeKey = new(ImGuiKey.DownArrow);

    private readonly TreeNode<TNodeData> _rootNode;
    private TreeNode<TNodeData> _selectedNode;

    private bool _isAnyNodeFocused;

    private KeyCommand _lastArrowKeyDown;
    private int _framesArrowKeyCounter;
    private bool _firstArrowSelection;

    private float _scrollY;

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

        var anyNodeHovered = false;
        if (Hexa.NET.ImGui.ImGui.BeginChild($"{Id}", new Vector2(contentRect.Width, contentRect.Height), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
        {
            float newScrollY = Hexa.NET.ImGui.ImGui.GetScrollY();

            if (_scrollY != newScrollY)
            {
                if (IsTabInactiveCore())
                    Hexa.NET.ImGui.ImGui.SetScrollY(_scrollY);

                _scrollY = newScrollY;
            }

            if (_isAnyNodeFocused)
                ChangeSelectedNodeOnArrowKey();

            bool isAnyNodeFocused = UpdateNodes(Nodes, ref anyNodeHovered);

            UpdateNodeFocusState(isAnyNodeFocused);
        }

        Hexa.NET.ImGui.ImGui.EndChild();
    }

    private bool UpdateNodes(IList<TreeNode<TNodeData>> nodes, ref bool nodeHovered)
    {
        var isAnyNodeFocused = false;
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
            isAnyNodeFocused |= Hexa.NET.ImGui.ImGui.IsItemFocused();

            Hexa.NET.ImGui.ImGui.PopStyleVar(2);

            bool changedExpansion = expanded != node.IsExpanded;
            if (changedExpansion && Enabled)
                node.IsExpanded = expanded;

            if (nodeFontPtr != null)
                Hexa.NET.ImGui.ImGui.PopFont();

            if (!node.TextColor.IsEmpty)
                Hexa.NET.ImGui.ImGui.PopStyleColor();

            Hexa.NET.ImGui.ImGui.PopID();

            // Check if tree node is hovered
            nodeHovered |= Hexa.NET.ImGui.ImGui.IsItemHovered();

            // Change selected node, if expansion of node did not change, and mouse is over node
            if (!changedExpansion && IsTreeNodeClicked() && SelectedNode != node && Enabled)
                SelectedNode = node;

            // Add context, only if mouse is over a tree node
            if (SelectedNode == node)
                ContextMenu?.Update();

            // Add children nodes, if parent is expanded
            if (node is { IsExpanded: true, Nodes.Count: > 0 })
                isAnyNodeFocused |= UpdateNodes(node.Nodes, ref nodeHovered);

            if (expanded)
                Hexa.NET.ImGui.ImGui.TreePop();
        }

        return isAnyNodeFocused;
    }

    private void UpdateNodeFocusState(bool isAnyNodeFocused)
    {
        if (!_isAnyNodeFocused && !isAnyNodeFocused)
            return;

        if (!_isAnyNodeFocused && isAnyNodeFocused)
            _isAnyNodeFocused = true;

        if (_isAnyNodeFocused && !isAnyNodeFocused)
            _isAnyNodeFocused = false;
    }

    private void ChangeSelectedNodeOnArrowKey()
    {
        UpdateArrowKeyState();
        UpdateSelectedNodeOnArrowKey();
    }

    private readonly IList<KeyCommand> _pressedArrows = new List<KeyCommand>(2);
    private void UpdateArrowKeyState()
    {
        if (PreviousNodeKey.IsDown() && !_pressedArrows.Contains(PreviousNodeKey) && Enabled)
            _pressedArrows.Add(PreviousNodeKey);
        if (NextNodeKey.IsDown() && !_pressedArrows.Contains(NextNodeKey) && Enabled)
            _pressedArrows.Add(NextNodeKey);
        if (PreviousNodeKey.IsReleased())
            _pressedArrows.Remove(PreviousNodeKey);
        if (NextNodeKey.IsReleased())
            _pressedArrows.Remove(NextNodeKey);

        KeyCommand currentArrowKeyDown = default;
        if (_pressedArrows.Count > 0)
            currentArrowKeyDown = _pressedArrows[^1];

        if (_lastArrowKeyDown != currentArrowKeyDown)
        {
            _firstArrowSelection = true;
            _framesArrowKeyCounter = 0;
        }
        else if (!_lastArrowKeyDown.IsEmpty)
        {
            if (_firstArrowSelection)
            {
                _firstArrowSelection = false;
                _framesArrowKeyCounter = FirstArrowSelectionDelta_;
            }

            if (_framesArrowKeyCounter == 0)
                _framesArrowKeyCounter = ArrowSelectionFrameDelta_;
            else
                _framesArrowKeyCounter--;
        }

        _lastArrowKeyDown = default;
        if (_pressedArrows.Count > 0)
            _lastArrowKeyDown = _pressedArrows[^1];
    }

    private void UpdateSelectedNodeOnArrowKey()
    {
        if (_lastArrowKeyDown.IsEmpty)
            return;

        if (_framesArrowKeyCounter != 0)
            return;

        if (_lastArrowKeyDown == PreviousNodeKey)
            SelectedNode = GetPreviousData() ?? SelectedNode;
        else if (_lastArrowKeyDown == NextNodeKey)
            SelectedNode = GetNextNode() ?? SelectedNode;
    }

    private TreeNode<TNodeData> GetPreviousData()
    {
        IList<TreeNode<TNodeData>> nodeList = SelectedNode.Parent.Nodes;
        int nodeIndex = nodeList.IndexOf(SelectedNode);

        if (nodeIndex - 1 < 0)
            return SelectedNode.IsRoot ? null : SelectedNode.Parent;

        TreeNode<TNodeData> previousNode = nodeList[nodeIndex - 1];
        while (previousNode.IsExpanded)
        {
            if (previousNode.Nodes.Count <= 0)
                break;

            previousNode = previousNode.Nodes[^1];
        }

        return previousNode;
    }

    private TreeNode<TNodeData> GetNextNode()
    {
        if (SelectedNode.IsExpanded && SelectedNode.Nodes.Count > 0)
            return SelectedNode.Nodes[0];

        IList<TreeNode<TNodeData>> nodeList = SelectedNode.Parent.Nodes;
        int nodeIndex = nodeList.IndexOf(SelectedNode);

        if (nodeIndex + 1 < nodeList.Count)
            return nodeList[nodeIndex + 1];

        while (!nodeList[nodeIndex].IsRoot)
        {
            TreeNode<TNodeData> parentNode = nodeList[nodeIndex].Parent;

            nodeList = parentNode.Parent.Nodes;
            nodeIndex = nodeList.IndexOf(parentNode);

            if (nodeIndex + 1 < nodeList.Count)
                return nodeList[nodeIndex + 1];
        }

        return null;
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