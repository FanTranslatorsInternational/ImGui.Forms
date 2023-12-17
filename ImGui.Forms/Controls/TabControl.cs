using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Controls
{
    public class TabControl : Component
    {
        private int _selectedTabPageCount;
        private TabPage _selectedPageTemp;
        private TabPage _selectedPage;

        private readonly List<TabPage> _pages = new List<TabPage>();

        public IReadOnlyList<TabPage> Pages => _pages;

        public TabPage SelectedPage
        {
            get => _selectedPageTemp ?? _selectedPage;
            set
            {
                _selectedTabPageCount = 2;
                _selectedPageTemp = value;
            }
        }

        #region Events

        public event EventHandler SelectedPageChanged;
        public event Func<object, RemovingEventArgs, Task> PageRemoving;
        public event EventHandler<RemoveEventArgs> PageRemoved;

        #endregion

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override async void UpdateInternal(Rectangle contentRect)
        {
            var wasManuallyChanged = _selectedPageTemp != null && _selectedTabPageCount-- > 0 && _selectedPageTemp != _selectedPage;

            if (ImGuiNET.ImGui.BeginTabBar($"{Id}", ImGuiTabBarFlags.None))
            {
                var toRemovePages = new HashSet<TabPage>();
                foreach (var page in Pages.ToArray())
                {
                    var pageFlags = ImGuiTabItemFlags.None;
                    if (page.HasChanges) pageFlags |= ImGuiTabItemFlags.UnsavedDocument;
                    if (wasManuallyChanged && _selectedPageTemp == page) pageFlags |= ImGuiTabItemFlags.SetSelected;

                    ImGuiNET.ImGui.PushID(Application.Instance.IdFactory.Get(page));

                    var stillOpen = true;
                    if (ImGuiNET.ImGui.BeginTabItem(page.Title, ref stillOpen, pageFlags) && !wasManuallyChanged)
                    {
                        // Check selected page status
                        var wasChanged = _selectedPage != page;

                        _selectedPageTemp = null;
                        _selectedPage = page;

                        if (wasChanged)
                            OnSelectedPageChanged();

                        // Remove tab page on middle mouse click
                        if (ImGuiNET.ImGui.IsItemHovered() && ImGuiNET.ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                            toRemovePages.Add(page);

                        // Draw content of tab page
                        var yPos = (int)ImGuiNET.ImGui.GetCursorPosY();

                        var pageWidth = page.Content.GetWidth(contentRect.Width);
                        var pageHeight = page.Content.GetHeight(contentRect.Height - yPos);
                        
                        if (ImGuiNET.ImGui.BeginChild($"##{Id}-in", contentRect.Size, ImGuiChildFlags.None, ImGuiWindowFlags.None))
                            page.Content.Update(new Rectangle(contentRect.X, contentRect.Y + yPos, pageWidth, pageHeight));

                        ImGuiNET.ImGui.EndChild();

                        ImGuiNET.ImGui.EndTabItem();
                    }

                    ImGuiNET.ImGui.PopID();

                    if (!stillOpen)
                        toRemovePages.Add(page);
                }

                ImGuiNET.ImGui.EndTabBar();

                // Handle pages to remove asynchronously
                foreach (var toRemove in toRemovePages)
                    await RemovePageInternal(toRemove);
            }
        }

        /// <summary>
        /// Adds a page to the <see cref="TabControl"/>
        /// </summary>
        /// <param name="page">the <see cref="TabPage"/> to add.</param>
        public void AddPage(TabPage page)
        {
            _pages.Add(page);
        }

        /// <summary>
        /// Removes a page from the <see cref="TabControl"/>.
        /// </summary>
        /// <param name="page">The <see cref="TabPage"/> to remove.</param>
        /// <remarks>Does not invoke <see cref="PageRemoving"/> and <see cref="PageRemoved"/>.</remarks>
        public void RemovePage(TabPage page)
        {
            if (_selectedPage == page)
                _selectedPage = null;

            _pages.Remove(page);
        }

        private async Task<bool> RemovePageInternal(TabPage page)
        {
            if (await OnPageRemoving(page))
                return false;

            if (_selectedPage == page)
                _selectedPage = null;

            _pages.Remove(page);
            OnPageRemoved(page);

            return true;
        }

        private void OnSelectedPageChanged()
        {
            SelectedPageChanged?.Invoke(this, new EventArgs());
        }

        private async Task<bool> OnPageRemoving(TabPage page)
        {
            var args = new RemovingEventArgs(page);
            if (PageRemoving != null)
                await PageRemoving.Invoke(this, args);

            return args.Cancel;
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
