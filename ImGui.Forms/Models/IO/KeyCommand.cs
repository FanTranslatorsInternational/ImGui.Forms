using ImGui.Forms.Support.Veldrid.ImGui;
using ImGuiNET;
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

        public ImGuiKey GetImGuiKey()
        {
            return _key == Key.Unknown ? ImGuiKey.None : GetKey(_key);
        }

        public ImGuiKey GetImGuiModifierKey()
        {
            return _modifiers == ModifierKeys.None ? ImGuiKey.ModNone : GetModifierKey(_modifiers);
        }

        // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
        public ImGuiKey GetImGuiKeyChord()
        {
            if (IsEmpty)
                return ImGuiKey.None;

            ImGuiKey result = GetKey(_key);
            if (_modifiers != ModifierKeys.None)
                result |= GetModifierKey(_modifiers);

            return result;
        }

        public static bool operator ==(KeyCommand a, KeyCommand b) => a._modifiers == b._modifiers && a._key == b._key;
        public static bool operator !=(KeyCommand a, KeyCommand b) => a._modifiers != b._modifiers || a._key != b._key;

        public override string ToString()
        {
            return $"Mod: {_modifiers}, Key: {_key}";
        }

        private ImGuiKey GetKey(Key key)
        {
            if (!ImGuiKeyMapper.TryMapKey(key, out ImGuiKey imGuiKey))
                return ImGuiKey.None;

            return imGuiKey;
        }

        private ImGuiKey GetModifierKey(ModifierKeys key)
        {
            if (!ImGuiKeyMapper.TryMapModifierKey(key, out ImGuiKey imGuiKey))
                return ImGuiKey.None;

            return imGuiKey;
        }
    }
}
