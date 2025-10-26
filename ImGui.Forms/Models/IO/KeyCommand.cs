using ImGui.Forms.Localization;
using ImGui.Forms.Support.Veldrid.ImGui;
using ImGuiNET;
using Veldrid;

namespace ImGui.Forms.Models.IO
{
    public readonly struct KeyCommand
    {
        private readonly ModifierKeys _modifiers;
        private readonly MouseButton? _mouse = null;
        private readonly Key _key;
        private readonly LocalizedString _name;

        public bool IsEmpty => !HasModifier && !HasKey && !HasMouse;

        public bool HasModifier => _modifiers != ModifierKeys.None;
        public bool HasMouse => _mouse.HasValue;
        public bool HasKey => _key != Key.Unknown;

        public string Name => _name;

        public KeyCommand(Key key, LocalizedString name = default) : this(ModifierKeys.None, key, null, name)
        { }

        public KeyCommand(MouseButton mouse, LocalizedString name = default) : this(ModifierKeys.None, Key.Unknown, mouse, name)
        { }

        public KeyCommand(ModifierKeys modifiers, Key key, LocalizedString name = default) : this(modifiers, key, null, name)
        { }

        public KeyCommand(ModifierKeys modifiers, MouseButton mouse, LocalizedString name = default) : this(modifiers, Key.Unknown, mouse, name)
        { }

        private KeyCommand(ModifierKeys modifiers, Key key, MouseButton? mouse, LocalizedString name = default)
        {
            _modifiers = modifiers;
            _mouse = mouse;
            _key = key;
            _name = name;
        }

        public bool IsPressed(bool onActiveLayer = true)
        {
            if (IsEmpty)
                return false;

            if (!IsActive(onActiveLayer))
                return false;

            if (HasMouse)
            {
                var mouse = GetImGuiMouseButton();
                if (mouse.HasValue && ImGuiNET.ImGui.IsMouseReleased(mouse.Value))
                    return !HasModifier && !HasKey || IsDown(onActiveLayer);

                return false;
            }

            var isPressed = true;

            if (!HasModifier && HasKey)
                isPressed = ImGuiNET.ImGui.IsKeyPressed(GetImGuiKey());
            else if (HasModifier && !HasKey)
                isPressed = ImGuiNET.ImGui.IsKeyPressed(GetImGuiModifierKey());
            else if (HasModifier && HasKey)
                isPressed = ImGuiNET.ImGui.IsKeyChordPressed(GetImGuiKeyChord());

            return isPressed;
        }

        public bool IsDown(bool onActiveLayer = true)
        {
            if (!HasModifier && !HasKey)
                return false;

            if (!IsActive(onActiveLayer))
                return false;

            var isDown = false;
            if (HasModifier && HasKey)
                isDown = ImGuiNET.ImGui.IsKeyDown(GetImGuiModifierKey()) && ImGuiNET.ImGui.IsKeyDown(GetImGuiKey());
            else if (HasModifier)
                isDown = ImGuiNET.ImGui.IsKeyDown(GetImGuiModifierKey());
            else if (HasKey)
                isDown = ImGuiNET.ImGui.IsKeyDown(GetImGuiKey());

            return isDown;
        }

        public bool IsReleased(bool onActiveLayer = true)
        {
            if (!HasModifier && !HasKey)
                return false;

            if (!IsActive(onActiveLayer))
                return false;

            var isReleased = false;
            if (HasModifier && HasKey)
                isReleased = ImGuiNET.ImGui.IsKeyReleased(GetImGuiModifierKey()) && ImGuiNET.ImGui.IsKeyReleased(GetImGuiKey());
            else if (HasModifier)
                isReleased = ImGuiNET.ImGui.IsKeyReleased(GetImGuiModifierKey());
            else if (HasKey)
                isReleased = ImGuiNET.ImGui.IsKeyReleased(GetImGuiKey());

            return isReleased;
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

        private ImGuiMouseButton? GetImGuiMouseButton()
        {
            if (!ImGuiKeyMapper.TryMapMouseButton(_mouse!.Value, out ImGuiMouseButton? imGuiMouse))
                return null;

            return imGuiMouse;
        }

        public static bool operator ==(KeyCommand a, KeyCommand b) => a._modifiers == b._modifiers && a._key == b._key;
        public static bool operator !=(KeyCommand a, KeyCommand b) => a._modifiers != b._modifiers || a._key != b._key;

        public override string ToString()
        {
            return $"Mod: {_modifiers}, Key: {_key}";
        }
    }
}
