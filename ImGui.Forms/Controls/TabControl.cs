using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Factories;
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

        private readonly Dictionary<TabPage, bool> _removeOverwrite = new();

        private readonly List<TabPage> _pages = new();

        #region Properties

        public IReadOnlyList<TabPage> Pages => _pages;

        public TabPage SelectedPage
        {
            get => _selectedPageTemp ?? _selectedPage;
            set
            {
                if (!Enabled)
                    return;

                _selectedTabPageCount = 2;
                _selectedPageTemp = value;
            }
        }

        #endregion

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
                foreach (TabPage page in Pages.ToArray())
                {
                    var pageFlags = ImGuiTabItemFlags.None;
                    if (page.HasChanges) pageFlags |= ImGuiTabItemFlags.UnsavedDocument;
                    if (IsSelected(page, wasManuallyChanged))
                        pageFlags |= ImGuiTabItemFlags.SetSelected;

                    ImGuiNET.ImGui.PushID(IdFactory.Get(page));

                    var stillOpen = true;
                    if (ImGuiNET.ImGui.BeginTabItem(page.Title, ref stillOpen, pageFlags))
                    {
                        if (!wasManuallyChanged)
                        {
                            // Check selected page status
                            var wasChanged = _selectedPage != page;

                            if (wasChanged && Enabled)
                            {
                                _selectedPage?.Content?.SetTabInactiveInternal();
                                _selectedPage = page;
                            }

                            _selectedPageTemp = null;

                            if (wasChanged && Enabled)
                                OnSelectedPageChanged();

                            // Remove tab page on middle mouse click
                            if (ImGuiNET.ImGui.IsItemHovered() && ImGuiNET.ImGui.IsMouseClicked(ImGuiMouseButton.Middle) && Enabled)
                                toRemovePages.Add(page);

                            // Draw content of tab page
                            var yPos = (int)ImGuiNET.ImGui.GetCursorPosY();

                            var pageWidth = page.Content.GetWidth(contentRect.Width, contentRect.Height - yPos);
                            var pageHeight = page.Content.GetHeight(contentRect.Width, contentRect.Height - yPos);

                            var pageSize = new Vector2(pageWidth, pageHeight);
                            if (ImGuiNET.ImGui.BeginChild($"##{Id}-in", pageSize, ImGuiChildFlags.None, ImGuiWindowFlags.None))
                            {
                                var pagePos = ImGuiNET.ImGui.GetWindowPos();

                                if (page.ShowBorder)
                                    ImGuiNET.ImGui.GetWindowDrawList().AddRect(pagePos, pagePos + pageSize, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Border));

                                page.Content.Update(new Rectangle((int)pagePos.X, (int)pagePos.Y, pageWidth, pageHeight));
                            }

                            ImGuiNET.ImGui.EndChild();

                            ImGuiNET.ImGui.EndTabItem();
                        }
                    }
                    else
                    {
                        // If tab could not be rendered by ImGui, but it should still be shown, set it to inactive
                        page.Content?.SetTabInactiveInternal();
                    }

                    ImGuiNET.ImGui.PopID();

                    if (!stillOpen && Enabled)
                        toRemovePages.Add(page);
                }

                ImGuiNET.ImGui.EndTabBar();

                // Handle pages to remove asynchronously
                foreach (TabPage toRemove in toRemovePages)
                {
                    if (!await RemovePageInternal(toRemove))
                        _removeOverwrite[toRemove] = (wasManuallyChanged && _selectedPageTemp == toRemove) || _selectedPage == toRemove;
                }
            }
        }

        /// <summary>
        /// Adds a page to the <see cref="TabControl"/>
        /// </summary>
        /// <param name="page">the <see cref="TabPage"/> to add.</param>
        public void AddPage(TabPage page)
        {
            if (!Enabled)
                return;

            _pages.Add(page);
        }

        /// <summary>
        /// Removes a page from the <see cref="TabControl"/>.
        /// </summary>
        /// <param name="page">The <see cref="TabPage"/> to remove.</param>
        /// <remarks>Does not invoke <see cref="PageRemoving"/> and <see cref="PageRemoved"/>.</remarks>
        public void RemovePage(TabPage page)
        {
            if (!Enabled)
                return;

            if (_selectedPage == page)
                _selectedPage = null;

            _pages.Remove(page);
        }

        private bool IsSelected(TabPage page, bool wasManuallyChanged)
        {
            if (wasManuallyChanged && _selectedPageTemp == page)
                return true;

            if (!Enabled && _selectedPage == page)
                return true;

            return _removeOverwrite.Remove(page, out bool isSelected) && isSelected;
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
            SelectedPageChanged?.Invoke(this, EventArgs.Empty);
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
