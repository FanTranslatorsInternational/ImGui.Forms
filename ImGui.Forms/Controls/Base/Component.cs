using System;
using ImGui.Forms.Factories;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid.Sdl2;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace ImGui.Forms.Controls.Base;

public abstract class Component
{
    private bool _tabInactive;

    #region Properties

    /// <summary>
    /// The Id for this component.
    /// </summary>
    public int Id => IdFactory.Get(this);

    /// <summary>
    /// Declares if a component is visible and should be drawn with its content.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Declares if a component should react to external events.
    /// </summary>
    /// <remarks>Components that implement their own events should refer to this property to determine if they should be invoked.</remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Allows drag and drop support for this component.
    /// </summary>
    public bool AllowDragDrop { get; set; }

    /// <summary>
    /// Determines if the component should have a visible border.
    /// </summary>
    public bool ShowBorder { get; set; }

    #endregion

    #region Events

    /// <summary>
    /// The event to intercept DragDrop actions.
    /// </summary>
    public event EventHandler<DragDropEvent[]> DragDrop;

    #endregion

    /// <summary>
    /// Updates the components content to ImGui.
    /// </summary>
    public void Update(Rectangle contentRect)
    {
        // Handle visibility
        if (!Visible)
        {
            _tabInactive = false;
            return;
        }

        // Handle drawing of component
        ImGuiNET.ImGui.PushID(Id);

        ApplyStyles();
        UpdateInternal(contentRect);
        RemoveStyles();

        ImGuiNET.ImGui.PopID();

        // Draw border
        if (ShowBorder)
            ImGuiNET.ImGui.GetWindowDrawList().AddRect(contentRect.Position, contentRect.Position + contentRect.Size, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Border));

        // Handle Drag and Drop after rendering, so drag drop events go from most nested to least nested control
        // HINT: Drag and Drop only proceeds on the top active layer of form and modal windows
        // HINT: Don't handle Drag and Drop if the component either doesn't allow it or the component is marked as disabled.
        if (!AllowDragDrop || !Enabled)
        {
            _tabInactive = false;
            return;
        }

        if (Application.Instance.MainForm.IsActiveLayer()
            && Application.Instance.TryGetDragDrop(contentRect, out DragDropEvent[] dragDrops))
            OnDragDrop(dragDrops);

        _tabInactive = false;
    }

    /// <summary>
    /// Determines if the current component is on an inactive <see cref="TabPage"/>.
    /// </summary>
    /// <returns>If the current component is on an inactive <see cref="TabPage"/>.</returns>
    protected bool IsTabInactiveCore()
    {
        return _tabInactive;
    }

    /// <summary>
    /// Gets the finalized width of this component.
    /// </summary>
    /// <param name="parentWidth">The width of the parent component.</param>
    /// <param name="parentHeight">The height of the parent component.</param>
    /// <param name="layoutCorrection">A correctional value for layouts.</param>
    /// <returns>The finalized width of this component.</returns>
    public int GetWidth(int parentWidth, int parentHeight, float layoutCorrection = 1f)
    {
        var width = GetSize().Width;
        return width.IsContentAligned ?
            GetContentWidth(parentWidth, parentHeight, layoutCorrection) :
            GetDimension(width, parentWidth, layoutCorrection);
    }

    /// <summary>
    /// Gets the width of this component, if it's content aligned.
    /// </summary>
    /// <param name="parentWidth">The width of the parent component.</param>
    /// <param name="parentHeight">The height of the parent component.</param>
    /// <param name="layoutCorrection">A correctional value for layouts.</param>
    /// <returns>The content width of this component.</returns>
    protected virtual int GetContentWidth(int parentWidth, int parentHeight, float layoutCorrection = 1f) => 0;

    /// <summary>
    /// Gets the finalized height of this component.
    /// </summary>
    /// <param name="parentWidth">The width of the parent component.</param>
    /// <param name="parentHeight">The height of the parent component.</param>
    /// <param name="layoutCorrection">A correctional value for layouts.</param>
    /// <returns>The finalized height of this component.</returns>
    public int GetHeight(int parentWidth, int parentHeight, float layoutCorrection = 1f)
    {
        var height = GetSize().Height;
        return height.IsContentAligned ?
            GetContentHeight(parentWidth, parentHeight, layoutCorrection) :
            GetDimension(height, parentHeight, layoutCorrection);
    }

    /// <summary>
    /// Gets the height of this component, if it's content aligned.
    /// </summary>
    /// <param name="parentWidth">The width of the parent component.</param>
    /// <param name="parentHeight">The height of the parent component.</param>
    /// <param name="layoutCorrection">A correctional value for layouts.</param>
    /// <returns>The content height of this component.</returns>
    protected virtual int GetContentHeight(int parentWidth, int parentHeight, float layoutCorrection = 1f) => 0;

    /// <summary>
    /// Gets the absolute or relative size of the component. Use <see cref="GetWidth"/> or <see cref="GetHeight"/> to get the finalized size.
    /// </summary>
    /// <returns>The absolute or relative size of this component.</returns>
    public abstract Size GetSize();

    /// <summary>
    /// Updates the component.
    /// </summary>
    /// <param name="contentRect">The rectangle in which to draw the component.</param>
    protected abstract void UpdateInternal(Rectangle contentRect);

    /// <summary>
    /// Applies any styles specific to this component, before <see cref="UpdateInternal"/> is invoked.
    /// </summary>
    protected virtual void ApplyStyles() { }

    /// <summary>
    /// Removes any styles specific to this component, after <see cref="UpdateInternal"/> is invoked.
    /// </summary>
    protected virtual void RemoveStyles() { }

    /// <summary>
    /// Used by <see cref="TabControl"/> to mark components as inactive,
    /// due to them not being selected as the active page.
    /// </summary>
    internal void SetTabInactiveInternal()
    {
        SetTabInactive();
    }

    /// <summary>
    /// Used by 3rd-party components to propagate <see cref="TabPage"/> inactivity to child components.
    /// </summary>
    public void SetTabInactive()
    {
        _tabInactive = true;
        SetTabInactiveCore();
    }

    /// <summary>
    /// Used by 3rd-party code and components to destroy resources.
    /// </summary>
    public virtual void Destroy() { }

    /// <summary>
    /// Propagate the inactivity state from a <see cref="TabControl"/>
    /// </summary>
    protected virtual void SetTabInactiveCore() { }

    /// <summary>
    /// Invoke the DragDrop event of this component.
    /// </summary>
    /// <param name="events">The drag drop objects received.</param>
    private void OnDragDrop(DragDropEvent[] events)
    {
        DragDrop?.Invoke(this, events);
    }

    /// <summary>
    /// Calculates the integer value of a <see cref="SizeValue"/>.
    /// </summary>
    /// <param name="dimensionValue">The value to calculate.</param>
    /// <param name="maxDimensionValue">The maximum to calculate relative <see cref="SizeValue"/>s against.</param>
    /// <param name="correction">The corrective value to calculate relative <see cref="SizeValue"/>s against.</param>
    /// <returns></returns>
    protected internal static int GetDimension(SizeValue dimensionValue, int maxDimensionValue, float correction = 1f)
    {
        if (dimensionValue.IsAbsolute)
            return (int)Math.Min(dimensionValue.Value, maxDimensionValue);

        return (int)Math.Floor(dimensionValue.Value * maxDimensionValue * correction);
    }
}