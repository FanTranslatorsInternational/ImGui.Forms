using System;
using System.Collections.Generic;
using System.Linq;
using ImGui.Forms.Components.Base;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Components
{
    public class TabControl : Component
    {
        private bool _setPageManually;
        private TabPage _selectedPage;

        private readonly List<TabPage> _pages = new List<TabPage>();

        public IReadOnlyList<TabPage> Pages => _pages;

        public TabPage SelectedPage
        {
            get => _selectedPage;
            set
            {
                _setPageManually = true;
                _selectedPage = value;
            }
        }

        #region Events

        public event EventHandler SelectedPageChanged;
        public event EventHandler<RemovingEventArgs> PageRemoving;
        public event EventHandler<RemoveEventArgs> PageRemoved;

        #endregion

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            if (ImGuiNET.ImGui.BeginTabBar(Id.ToString(), ImGuiTabBarFlags.None))
            {
                foreach (var page in Pages.ToArray())
                {
                    var pageFlags = ImGuiTabItemFlags.None;
                    if (page.HasChanges) pageFlags |= ImGuiTabItemFlags.UnsavedDocument;
                    if (_setPageManually && _selectedPage == page) pageFlags |= ImGuiTabItemFlags.SetSelected;

                    var stillOpen = true;
                    if (ImGuiNET.ImGui.BeginTabItem(page.Title ?? string.Empty, ref stillOpen, pageFlags))
                    {
                        // Check selected page status
                        var wasChanged = _selectedPage != page;
                        _selectedPage = page;

                        if (wasChanged)
                            OnSelectedPageChanged();

                        if (ImGuiNET.ImGui.IsItemHovered() && ImGuiNET.ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                            if (RemovePage(page))
                            {
                                ImGuiNET.ImGui.EndTabItem();
                                continue;
                            }

                        var pageWidth = page.Content.GetWidth(contentRect.Width);
                        var pageHeight = page.Content.GetHeight(contentRect.Height - (int)ImGuiNET.ImGui.GetCursorPosY());

                        page.Content.Update(new Rectangle(contentRect.X, contentRect.Y + (int)ImGuiNET.ImGui.GetCursorPosY(), pageWidth, pageHeight));

                        ImGuiNET.ImGui.EndTabItem();
                    }

                    if (!stillOpen)
                        RemovePage(page);
                }

                ImGuiNET.ImGui.EndTabBar();

                if (_setPageManually)
                    _setPageManually = false;
            }
        }

        public void AddPage(TabPage page)
        {
            _pages.Add(page);
        }

        public bool RemovePage(TabPage page)
        {
            OnPageRemoving(page, out var cancel);
            if (cancel)
                return false;

            _pages.Remove(page);
            OnPageRemoved(page);

            return true;
        }

        private void OnSelectedPageChanged()
        {
            SelectedPageChanged?.Invoke(this, new EventArgs());
        }

        private void OnPageRemoving(TabPage page, out bool cancel)
        {
            var args = new RemovingEventArgs(page);
            PageRemoving?.Invoke(this, args);

            cancel = args.Cancel;
        }

        private void OnPageRemoved(TabPage page)
        {
            PageRemoved?.Invoke(this, new RemoveEventArgs(page));
        }
    }

    public class RemoveEventArgs : EventArgs
    {
        public TabPage Page { get; }

        public RemoveEventArgs(TabPage page)
        {
            Page = page;
        }
    }

    public class RemovingEventArgs : RemoveEventArgs
    {
        public bool Cancel { get; set; }

        public RemovingEventArgs(TabPage page) : base(page)
        {
        }
    }
}
