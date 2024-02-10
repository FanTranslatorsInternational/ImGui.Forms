using Veldrid;

namespace ImGui.Forms.Models.IO
{
    public readonly struct KeyCommand
    {
        private readonly ModifierKeys _modifiers;
        private readonly Key _key;

        public bool IsEmpty => _modifiers == 0 && _key == 0;

        public KeyCommand(Key key) : this(ModifierKeys.None, key)
        { }

        public KeyCommand(ModifierKeys modifiers, Key key)
        {
            _modifiers = modifiers;
            _key = key;
        }

        public static bool operator ==(KeyCommand a, KeyCommand b) => a._modifiers == b._modifiers && a._key == b._key;
        public static bool operator !=(KeyCommand a, KeyCommand b) => a._modifiers != b._modifiers || a._key != b._key;

        public override string ToString()
        {
            return $"Mod: {_modifiers}, Key: {_key}";
        }
    }
}
