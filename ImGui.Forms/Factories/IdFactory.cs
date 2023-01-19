using System;
using System.Collections.Generic;

namespace ImGui.Forms.Factories
{
    public class IdFactory
    {
        private readonly Random _random;
        private readonly HashSet<int> _ids;
        private readonly IDictionary<object, (int, bool)> _idDictionary;

        internal IdFactory()
        {
            _random = new Random();
            _ids = new HashSet<int>();
            _idDictionary = new Dictionary<object, (int, bool)>();
        }

        internal void FreeIds()
        {
            // Remove unused Id's
            var objToDelete = new List<object>();
            foreach (var obj in _idDictionary)
            {
                if (obj.Value.Item2)
                {
                    // Reset frame markers
                    _idDictionary[obj.Key] = (_idDictionary[obj.Key].Item1, false);
                    continue;
                }

                _ids.Remove(obj.Value.Item1);
                objToDelete.Add(obj.Key);
            }

            foreach (var obj in objToDelete)
                _idDictionary.Remove(obj);
        }

        public int Get(object item)
        {
            if (_idDictionary.ContainsKey(item))
            {
                _idDictionary[item] = (_idDictionary[item].Item1, true);
                return _idDictionary[item].Item1;
            }

            var id = Create();

            _ids.Add(id);
            _idDictionary[item] = (id, true);

            return id;
        }

        private int Create()
        {
            var id = _random.Next(int.MaxValue);
            while (_ids.Contains(id))
                id = _random.Next(int.MaxValue);

            return id;
        }
    }
}
