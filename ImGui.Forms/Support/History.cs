using System;
using System.Collections.Generic;

namespace ImGui.Forms.Support
{
    class History<T>
    {
        private readonly IList<T> _items;
        private int _index;
        private readonly int _capacity;

        public History(T initialItem, int capacity = 256)
        {
            _capacity = Math.Max(1, capacity);

            _items = new List<T>(_capacity) { initialItem };
        }

        public void PushItem(T item)
        {
            // Remove all items after step
            var count = _items.Count;
            for (var i = _index + 1; i < count; i++)
                _items.RemoveAt(_index + 1);

            // If capacity is reached, move first item out of list
            if (_items.Count >= _capacity)
            {
                _items.RemoveAt(0);
                _index--;
            }

            // Move in new item
            _items.Add(item);
            _index++;
        }

        public void MoveForward()
        {
            if (IsLastItem())
                return;

            _index++;
        }

        public void MoveBackward()
        {
            if (IsFirstItem())
                return;

            _index--;
        }

        public bool IsFirstItem()
        {
            return _index == 0;
        }

        public bool IsLastItem()
        {
            return _index + 1 == _items.Count;
        }

        public T GetCurrentItem()
        {
            return _items[_index];
        }
    }
}
