using Hexa.NET.ImGui;
using ImGui.Forms.Localization;

namespace ImGui.Forms.Models.IO;

public readonly struct KeyCommand
{
    private readonly ImGuiKey _modifiers;
    private readonly ImGuiMouseButton? _mouse = null;
    private readonly ImGuiKey _key;
    private readonly LocalizedString _name;

    public bool IsEmpty => !HasModifier && !HasKey && !HasMouse;

    public bool HasModifier => _modifiers != ImGuiKey.None;
    public bool HasMouse => _mouse.HasValue;
    public bool HasKey => _key != ImGuiKey.None;

    public string Name => _name;

    public KeyCommand(ImGuiKey key, LocalizedString name = default) : this(ImGuiKey.None, key, null, name)
    { }

    public KeyCommand(ImGuiMouseButton mouse, LocalizedString name = default) : this(ImGuiKey.None, ImGuiKey.None, mouse, name)
    { }

    public KeyCommand(ImGuiKey modifiers, ImGuiKey key, LocalizedString name = default) : this(modifiers, key, null, name)
    { }

    public KeyCommand(ImGuiKey modifiers, ImGuiMouseButton mouse, LocalizedString name = default) : this(modifiers, ImGuiKey.None, mouse, name)
    { }

    private KeyCommand(ImGuiKey modifiers, ImGuiKey key, ImGuiMouseButton? mouse, LocalizedString name = default)
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
            if (mouse.HasValue && Hexa.NET.ImGui.ImGui.IsMouseReleased(mouse.Value))
                return !HasModifier && !HasKey || IsDown(onActiveLayer);

            return false;
        }

        var isPressed = true;

        if (!HasModifier && HasKey)
            isPressed = Hexa.NET.ImGui.ImGui.IsKeyPressed(GetImGuiKey());
        else if (HasModifier && !HasKey)
            isPressed = Hexa.NET.ImGui.ImGui.IsKeyPressed(GetImGuiModifierKey());
        else if (HasModifier && HasKey)
            isPressed = Hexa.NET.ImGui.ImGui.IsKeyChordPressed(GetImGuiKeyChord());

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
            isDown = Hexa.NET.ImGui.ImGui.IsKeyDown(GetImGuiModifierKey()) && Hexa.NET.ImGui.ImGui.IsKeyDown(GetImGuiKey());
        else if (HasModifier)
            isDown = Hexa.NET.ImGui.ImGui.IsKeyDown(GetImGuiModifierKey());
        else if (HasKey)
            isDown = Hexa.NET.ImGui.ImGui.IsKeyDown(GetImGuiKey());

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
            isReleased = Hexa.NET.ImGui.ImGui.IsKeyReleased(GetImGuiModifierKey()) && Hexa.NET.ImGui.ImGui.IsKeyReleased(GetImGuiKey());
        else if (HasModifier)
            isReleased = Hexa.NET.ImGui.ImGui.IsKeyReleased(GetImGuiModifierKey());
        else if (HasKey)
            isReleased = Hexa.NET.ImGui.ImGui.IsKeyReleased(GetImGuiKey());

        return isReleased;
    }

    private bool IsActive(bool onActiveLayer)
    {
        return !onActiveLayer || Application.Instance.MainForm.IsActiveLayer();
    }

    // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
    private int GetImGuiKeyChord()
    {
        ImGuiKey result = GetImGuiKey();
        return (int)(result | GetImGuiModifierKey());
    }

    private ImGuiKey GetImGuiKey()
    {
        return _key;
    }

    private ImGuiKey GetImGuiModifierKey()
    {
        return _modifiers;
    }

    private ImGuiMouseButton? GetImGuiMouseButton()
    {
        return _mouse;
    }

    public static bool operator ==(KeyCommand a, KeyCommand b) => a._modifiers == b._modifiers && a._key == b._key;
    public static bool operator !=(KeyCommand a, KeyCommand b) => a._modifiers != b._modifiers || a._key != b._key;

    public override string ToString()
    {
        return $"Mod: {_modifiers}, Key: {_key}";
    }
}