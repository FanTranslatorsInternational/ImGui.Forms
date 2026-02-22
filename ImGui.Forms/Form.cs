using Hexa.NET.ImGui;
using Hexa.NET.SDL3;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Extensions;
using ImGui.Forms.Factories;
using ImGui.Forms.Localization;
using ImGui.Forms.Modals;
using ImGui.Forms.Resources;
using ImGui.Forms.Support;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Rectangle = ImGui.Forms.Support.Rectangle;

namespace ImGui.Forms;

// HINT: Does not derive from Component to not be a component and therefore nestable into other containers
public abstract class Form
{
    private readonly IList<Modal> _modals = [];

    private Image<Rgba32>? _icon;
    private bool _setIcon;
    private Modal? _modalRendering;

    #region Properties

    public int Id => Application.Instance.Ids.Get(this);

    public LocalizedString Title { get; set; } = string.Empty;
    public Vector2 Size { get; set; } = new(700, 400);
    public int Width => (int)Size.X;
    public int Height => (int)Size.Y;

    public bool AllowDragDrop { get; set; }

    /// <summary>
    /// Gets and sets the applications icon.
    /// </summary>
    /// <remarks>The icon dimensions need to be a power of 2 (eg. 32, 64, 128, etc)</remarks>
    public Image<Rgba32>? Icon
    {
        get => _icon;
        protected set
        {
            _icon = value;
            _setIcon = true;
        }
    }

    public MainMenuBar? MenuBar { get; protected set; }

    public Component? Content { get; protected set; }

    public Vector2 Padding
    {
        get => Style.GetStyleVector2(ImGuiStyleVar.WindowPadding);
        set => Style.SetStyle(ImGuiStyleVar.WindowPadding, value);
    }

    public FontResource DefaultFont { get; set; } = FontFactory.GetDefault(13);

    #endregion

    #region Events

    public event EventHandler<string[]> DragDrop;
    public event EventHandler Load;
    public event EventHandler Resized;
    public event Func<object, ClosingEventArgs, Task> Closing;

    #endregion

    internal void PushModal(Modal modal)
    {
        if (_modals.Count > 0)
            _modals.Last().ChildModal = modal;

        _modals.Add(modal);
    }

    internal void PopModal()
    {
        if (_modals.Count <= 0)
            return;

        _modals.Remove(_modals.Last());

        if (_modals.Count > 0)
            _modals.Last().ChildModal = null;
    }

    internal bool IsActiveLayer()
    {
        if (_modals.Count <= 0)
            return true;

        return _modals.Last() == _modalRendering;
    }

    internal void SetRenderingModal(Modal? modal)
    {
        _modalRendering = modal;
    }

    internal bool HasBlockingModals()
    {
        return _modals.Any(x => x.BlockFormClosing);
    }

    public bool HasOpenModals()
    {
        return _modals.Count > 0;
    }

    internal void Update()
    {
        // Begin window
        Hexa.NET.ImGui.ImGui.Begin($"{Id}", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove);
        
        // Set nearest sampler for image interpolation
        ImGuiSampler.SetNearest();

        // Set icon
        if (_setIcon)
        {
            Sdl2NativeExtensions.SetWindowIcon(Application.Instance.Window!.Value, Icon);
            _setIcon = false;
        }

        float mainScale = SDL.GetDisplayContentScale(SDL.GetPrimaryDisplay());

        // Set up styles
        Style.ApplyStyle();

        var style = Hexa.NET.ImGui.ImGui.GetStyle();
        style.ScaleAllSizes(mainScale);
        style.FontScaleDpi = mainScale;

        style.WindowRounding = 0;
        style.FrameBorderSize = 0;
        style.WindowBorderSize = 0;

        SDL.SetWindowTitle(Application.Instance.Window!.Value, Title);

        Hexa.NET.ImGui.ImGui.SetWindowSize(Size, ImGuiCond.Always);

        // Push font to default to
        ImFontPtr? fontPtr = DefaultFont.GetPointer();
        if (fontPtr != null)
            Hexa.NET.ImGui.ImGui.PushFont(fontPtr.Value, DefaultFont.Data.Size);

        // Add menu bar
        MenuBar?.Update();
        var menuHeight = MenuBar?.Height ?? 0;

        // Add form controls
        Hexa.NET.ImGui.ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Padding);
        Hexa.NET.ImGui.ImGui.SetWindowPos(new Vector2(0, menuHeight));

        var contentPos = Hexa.NET.ImGui.ImGui.GetCursorScreenPos();
        var contentWidth = Content?.GetWidth(Width - (int)Padding.X * 2, Height - (int)Padding.Y * 2 - menuHeight) ?? 0;
        var contentHeight = Content?.GetHeight(Width - (int)Padding.X * 2, Height - (int)Padding.Y * 2 - menuHeight) ?? 0;
        var contentRect = new Rectangle(contentPos, new Vector2(contentWidth, contentHeight));

        Content?.Update(contentRect);

        // Add modal
        var modal = _modals.Count > 0 ? _modals.First() : null;
        Modal.DrawModal(modal);

        SetRenderingModal(null);

        // Handle Drag and Drop after rendering only if form is the top active layer
        if (AllowDragDrop && _modals.Count <= 0)
            if (Application.Instance.TryGetDragDrop(new Rectangle(Vector2.Zero, Size), out string[] files))
                OnDragDrop(files);

        Hexa.NET.ImGui.ImGui.PopStyleVar();

        if (fontPtr != null)
            Hexa.NET.ImGui.ImGui.PopFont();

        // End window
        Hexa.NET.ImGui.ImGui.End();
    }

    protected void Close()
    {
        Application.Instance.Exit();
    }

    #region Event Invokers

    internal void OnResized()
    {
        Resized?.Invoke(this, EventArgs.Empty);
    }

    internal void OnLoad()
    {
        Load?.Invoke(this, EventArgs.Empty);
    }

    internal async Task OnClosing(ClosingEventArgs e)
    {
        if (Closing == null)
            return;

        await Closing?.Invoke(this, e);
    }

    private void OnDragDrop(string[] files)
    {
        DragDrop?.Invoke(this, files);
    }

    #endregion
}

public class ClosingEventArgs : EventArgs
{
    public bool Cancel { get; set; }
}