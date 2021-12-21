using System;
using System.Collections.Generic;

namespace ImGui.Forms.Factories
{
    public class IdFactory
    {
        private readonly Random _random;
        private readonly HashSet<int> _ids;
        private readonly IDictionary<object, int> _idDictionary;

        internal IdFactory()
        {
            _random = new Random();
            _ids = new HashSet<int>();
            _idDictionary = new Dictionary<object, int>();
        }

        public int Get(object item)
        {
            if (_idDictionary.ContainsKey(item))
                return _idDictionary[item];

            var id = Create();

            _ids.Add(id);
            _idDictionary[item] = id;

            return id;
        }

        public int Create()
        {
            var id= _random.Next(int.MaxValue);
            while (_ids.Contains(id))
                id = _random.Next(int.MaxValue);

            return id;
        }
    }
}
