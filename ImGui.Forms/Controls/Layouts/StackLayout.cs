using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using Veldrid;

namespace ImGui.Forms.Controls.Layouts
{
    public class StackLayout : Component
    {
        private readonly TableLayout _tableLayout;
        private Size _size = Size.Parent;

        public IList<StackItem> Items { get; }

        public Alignment Alignment { get; set; } = Alignment.Vertical;

        public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

        public int ItemSpacing
        {
            get => (int)_tableLayout.Spacing.X;
            set => _tableLayout.Spacing = new Vector2(value, value);
        }

        public Size Size
        {
            get => _size;
            set
            {
                _size = value;
                _tableLayout.Size = value;
            }
        }

        public StackLayout()
        {
            var itemList = new ObservableList<StackItem>();
            itemList.ItemAdded += (s, e) => AddItem(e.Item);
            itemList.ItemRemoved += (s, e) => RemoveItem(e.Item);

            Items = itemList;
            _tableLayout = new TableLayout();
        }

        public override Size GetSize()
        {
            return _size;
        }

        public override int GetWidth(int parentWidth, float layoutCorrection = 1)
        {
            return _tableLayout.GetWidth(parentWidth, layoutCorrection);
        }

        public override int GetHeight(int parentHeight, float layoutCorrection = 1)
        {
            return _tableLayout.GetHeight(parentHeight, layoutCorrection);
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            // Update placeholder for proper stack alignment
            UpdatePlaceholder(contentRect);

            // Update layout
            _tableLayout.Update(contentRect);
        }

        private void UpdatePlaceholder(Rectangle contentRect)
        {
            switch (Alignment)
            {
                case Alignment.Horizontal:
                    if (_tableLayout.Rows.Count <= 0 || _tableLayout.Rows[0].Cells[0].Content is Placeholder)
                        return;

                    var emptyWidth = contentRect.Width - _tableLayout.GetWidth(contentRect.Width) - _tableLayout.Spacing.X;
                    switch (HorizontalAlignment)
                    {
                        case HorizontalAlignment.Center:
                            _tableLayout.Rows[0].Cells.Insert(0, new Placeholder(new Size(emptyWidth / 2, -1)));
                            break;

                        case HorizontalAlignment.Right:
                            _tableLayout.Rows[0].Cells.Insert(0, new Placeholder(new Size(emptyWidth, -1)));
                            break;
                    }
                    break;

                case Alignment.Vertical:
                    break;
            }
        }

        private void AddItem(StackItem item)
        {
            switch (Alignment)
            {
                case Alignment.Horizontal:
                    if (_tableLayout.Rows.Count == 0)
                    {
                        var row = new TableRow();
                        _tableLayout.Rows.Add(row);
                    }

                    _tableLayout.Rows[0].Cells.Add(item);

                    if (_tableLayout.Rows[0].Cells[0].Content is Placeholder)
                        _tableLayout.Rows[0].Cells.RemoveAt(0);

                    break;

                case Alignment.Vertical:
                    _tableLayout.Rows.Add(new TableRow { Cells = { item } });
                    break;
            }
        }

        private void RemoveItem(StackItem item)
        {
            switch (Alignment)
            {
                case Alignment.Horizontal:
                    if (_tableLayout.Rows.Count == 0)
                        return;

                    foreach (var cell in _tableLayout.Rows[0].Cells)
                        if (cell.Content == item.Content)
                        {
                            _tableLayout.Rows[0].Cells.Remove(cell);
                            break;
                        }

                    if (_tableLayout.Rows[0].Cells[0].Content is Placeholder)
                        _tableLayout.Rows[0].Cells.RemoveAt(0);

                    break;

                case Alignment.Vertical:
                    foreach (var row in _tableLayout.Rows)
                        if (row.Cells.Count > 0 && row.Cells[0].Content == item.Content)
                        {
                            _tableLayout.Rows.Remove(row);
                            break;
                        }

                    break;
            }
        }

        class Placeholder : Component
        {
            private readonly Size _size;

            public Placeholder(Size size)
            {
                _size = size;
            }

            public override Size GetSize()
            {
                return _size;
            }

            protected override void UpdateInternal(Rectangle contentRect)
            {
            }
        }
    }

    class ObservableList<TItem> : IList<TItem>
    {
        private readonly IList<TItem> _items = new List<TItem>();

        public int Count => _items.Count;
        public bool IsReadOnly => _items.IsReadOnly;

        public event EventHandler<ItemEventArgs<TItem>> ItemAdded;
        public event EventHandler<ItemEventArgs<TItem>> ItemRemoved;

        public IEnumerator<TItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TItem item)
        {
            _items.Add(item);
            OnItemAdded(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(TItem item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(TItem[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public bool Remove(TItem item)
        {
            var result = _items.Remove(item);
            OnItemRemoved(item);

            return result;
        }

        public int IndexOf(TItem item)
        {
            return _items.IndexOf(item);
        }

        public void Insert(int index, TItem item)
        {
            _items.Insert(index, item);
            OnItemAdded(item);
        }

        public void RemoveAt(int index)
        {
            var removedItem = _items[index];

            _items.RemoveAt(index);
            OnItemRemoved(removedItem);
        }

        public TItem this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        private void OnItemAdded(TItem item)
        {
            ItemAdded?.Invoke(this, new ItemEventArgs<TItem>(item));
        }

        private void OnItemRemoved(TItem item)
        {
            ItemRemoved?.Invoke(this, new ItemEventArgs<TItem>(item));
        }
    }

    class ItemEventArgs<TItem> : EventArgs
    {
        public TItem Item { get; }

        public ItemEventArgs(TItem item)
        {
            Item = item;
        }
    }

    public enum Alignment
    {
        Horizontal,
        Vertical
    }
}
