using ImGui.Forms.Support.Veldrid.ImGui;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Models.IO
{
    public readonly struct KeyCommand
    {
        private readonly ModifierKeys _modifiers;
        private readonly Key _key;

        public bool IsEmpty => !HasModifier && !HasKey;

        public bool HasModifier => _modifiers != ModifierKeys.None;
        public bool HasKey => _key != Key.Unknown;

        public KeyCommand(Key key) : this(ModifierKeys.None, key)
        { }

        public KeyCommand(ModifierKeys modifiers, Key key)
        {
            _modifiers = modifiers;
            _key = key;
        }

        public bool IsPressed(bool onActiveLayer = true)
        {
            if (IsEmpty)
                return false;

            if (!HasModifier)
                return IsActive(onActiveLayer) && ImGuiNET.ImGui.IsKeyPressed(GetImGuiKey());

            if (!HasKey)
                return IsActive(onActiveLayer) && ImGuiNET.ImGui.IsKeyPressed(GetImGuiModifierKey());

            return IsActive(onActiveLayer) && ImGuiNET.ImGui.IsKeyChordPressed(GetImGuiKeyChord());

        }

        public bool IsDown(bool onActiveLayer = true)
        {
            if (IsEmpty)
                return false;

            if (HasKey)
                return IsActive(onActiveLayer) && ImGuiNET.ImGui.IsKeyDown(GetImGuiKey());

            return IsActive(onActiveLayer) && ImGuiNET.ImGui.IsKeyDown(GetImGuiModifierKey());
        }

        public bool IsReleased(bool onActiveLayer = true)
        {
            if (IsEmpty)
                return false;

            if (HasKey)
                return IsActive(onActiveLayer) && ImGuiNET.ImGui.IsKeyReleased(GetImGuiKey());

            return IsActive(onActiveLayer) && ImGuiNET.ImGui.IsKeyReleased(GetImGuiModifierKey());
        }

        private bool IsActive(bool onActiveLayer)
        {
            return !onActiveLayer || Application.Instance.MainForm.IsActiveLayer();
        }

        // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
        private ImGuiKey GetImGuiKeyChord()
        {
            ImGuiKey result = GetImGuiKey();
            return result | GetImGuiModifierKey();
        }

        private ImGuiKey GetImGuiKey()
        {
            if (!ImGuiKeyMapper.TryMapKey(_key, out ImGuiKey imGuiKey))
                return ImGuiKey.None;

            return imGuiKey;
        }

        private ImGuiKey GetImGuiModifierKey()
        {
            if (!ImGuiKeyMapper.TryMapModifierKey(_modifiers, out ImGuiKey imGuiKey))
                return ImGuiKey.ModNone;

            return imGuiKey;
        }

        public static bool operator ==(KeyCommand a, KeyCommand b) => a._modifiers == b._modifiers && a._key == b._key;
        public static bool operator !=(KeyCommand a, KeyCommand b) => a._modifiers != b._modifiers || a._key != b._key;

        public override string ToString()
        {
            return $"Mod: {_modifiers}, Key: {_key}";
        }
    }
}
