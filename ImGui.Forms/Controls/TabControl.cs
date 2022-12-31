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
        public event Func<object, RemovingEventArgs, Task> PageRemoving;
        public event EventHandler<RemoveEventArgs> PageRemoved;

        #endregion

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override async void UpdateInternal(Rectangle contentRect)
        {
            if (ImGuiNET.ImGui.BeginTabBar(Id.ToString(), ImGuiTabBarFlags.None))
            {
                var toRemovePages = new HashSet<TabPage>();
                foreach (var page in Pages.ToArray())
                {
                    var pageFlags = ImGuiTabItemFlags.None;
                    if (page.HasChanges) pageFlags |= ImGuiTabItemFlags.UnsavedDocument;
                    if (_setPageManually && _selectedPage == page) pageFlags |= ImGuiTabItemFlags.SetSelected;

                    ImGuiNET.ImGui.PushID(Application.Instance.IdFactory.Get(page));

                    var stillOpen = true;
                    if (ImGuiNET.ImGui.BeginTabItem(page.Title, ref stillOpen, pageFlags))
                    {
                        // Check selected page status
                        var wasChanged = _selectedPage != page;
                        _selectedPage = page;

                        if (wasChanged)
                            OnSelectedPageChanged();

                        if (ImGuiNET.ImGui.IsItemHovered() && ImGuiNET.ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                            toRemovePages.Add(page);

                        var pageWidth = page.Content.GetWidth(contentRect.Width);
                        var pageHeight = page.Content.GetHeight(contentRect.Height - (int)ImGuiNET.ImGui.GetCursorPosY());

                        page.Content.Update(new Rectangle(contentRect.X, contentRect.Y + (int)ImGuiNET.ImGui.GetCursorPosY(), pageWidth, pageHeight));

                        ImGuiNET.ImGui.EndTabItem();
                    }

                    ImGuiNET.ImGui.PopID();

                    if (!stillOpen)
                        toRemovePages.Add(page);
                }

                ImGuiNET.ImGui.EndTabBar();

                if (_setPageManually)
                    _setPageManually = false;

                // Handle pages to remove asynchronously
                foreach (var toRemove in toRemovePages)
                    await RemovePage(toRemove);
            }
        }

        public void AddPage(TabPage page)
        {
            _pages.Add(page);
        }

        public async Task<bool> RemovePage(TabPage page)
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
