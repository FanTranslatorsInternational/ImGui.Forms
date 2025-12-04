using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;

namespace ImGui.Forms.Support.Veldrid.ImGui;

internal static class ImGuiKeyMapper
{
    public static bool TryMapKey(Key key, out ImGuiKey result)
    {
        ImGuiKey keyToImGuiKeyShortcut(Key keyToConvert, Key startKey1, ImGuiKey startKey2)
        {
            int changeFromStart1 = (int)keyToConvert - (int)startKey1;
            return startKey2 + changeFromStart1;
        }

        if (key is >= Key.F1 and <= Key.F12)
        {
            result = keyToImGuiKeyShortcut(key, Key.F1, ImGuiKey.F1);
            return true;
        }

        if (key is >= Key.Keypad0 and <= Key.Keypad9)
        {
            result = keyToImGuiKeyShortcut(key, Key.Keypad0, ImGuiKey.Keypad0);
            return true;
        }

        if (key is >= Key.A and <= Key.Z)
        {
            result = keyToImGuiKeyShortcut(key, Key.A, ImGuiKey.A);
            return true;
        }

        if (key is >= Key.Number0 and <= Key.Number9)
        {
            result = keyToImGuiKeyShortcut(key, Key.Number0, ImGuiKey._0);
            return true;
        }

        switch (key)
        {
            case Key.ShiftLeft:
            case Key.ShiftRight:
                result = ImGuiKey.ModShift;
                return true;
            case Key.ControlLeft:
            case Key.ControlRight:
                result = ImGuiKey.ModCtrl;
                return true;
            case Key.AltLeft:
            case Key.AltRight:
                result = ImGuiKey.ModAlt;
                return true;
            case Key.WinLeft:
            case Key.WinRight:
                result = ImGuiKey.ModSuper;
                return true;
            case Key.Menu:
                result = ImGuiKey.Menu;
                return true;
            case Key.Up:
                result = ImGuiKey.UpArrow;
                return true;
            case Key.Down:
                result = ImGuiKey.DownArrow;
                return true;
            case Key.Left:
                result = ImGuiKey.LeftArrow;
                return true;
            case Key.Right:
                result = ImGuiKey.RightArrow;
                return true;
            case Key.Enter:
                result = ImGuiKey.Enter;
                return true;
            case Key.Escape:
                result = ImGuiKey.Escape;
                return true;
            case Key.Space:
                result = ImGuiKey.Space;
                return true;
            case Key.Tab:
                result = ImGuiKey.Tab;
                return true;
            case Key.BackSpace:
                result = ImGuiKey.Backspace;
                return true;
            case Key.Insert:
                result = ImGuiKey.Insert;
                return true;
            case Key.Delete:
                result = ImGuiKey.Delete;
                return true;
            case Key.PageUp:
                result = ImGuiKey.PageUp;
                return true;
            case Key.PageDown:
                result = ImGuiKey.PageDown;
                return true;
            case Key.Home:
                result = ImGuiKey.Home;
                return true;
            case Key.End:
                result = ImGuiKey.End;
                return true;
            case Key.CapsLock:
                result = ImGuiKey.CapsLock;
                return true;
            case Key.ScrollLock:
                result = ImGuiKey.ScrollLock;
                return true;
            case Key.PrintScreen:
                result = ImGuiKey.PrintScreen;
                return true;
            case Key.Pause:
                result = ImGuiKey.Pause;
                return true;
            case Key.NumLock:
                result = ImGuiKey.NumLock;
                return true;
            case Key.KeypadDivide:
                result = ImGuiKey.KeypadDivide;
                return true;
            case Key.KeypadMultiply:
                result = ImGuiKey.KeypadMultiply;
                return true;
            case Key.KeypadSubtract:
                result = ImGuiKey.KeypadSubtract;
                return true;
            case Key.KeypadAdd:
                result = ImGuiKey.KeypadAdd;
                return true;
            case Key.KeypadDecimal:
                result = ImGuiKey.KeypadDecimal;
                return true;
            case Key.KeypadEnter:
                result = ImGuiKey.KeypadEnter;
                return true;
            case Key.Tilde:
                result = ImGuiKey.GraveAccent;
                return true;
            case Key.Minus:
                result = ImGuiKey.Minus;
                return true;
            case Key.Plus:
                result = ImGuiKey.Equal;
                return true;
            case Key.BracketLeft:
                result = ImGuiKey.LeftBracket;
                return true;
            case Key.BracketRight:
                result = ImGuiKey.RightBracket;
                return true;
            case Key.Semicolon:
                result = ImGuiKey.Semicolon;
                return true;
            case Key.Quote:
                result = ImGuiKey.Apostrophe;
                return true;
            case Key.Comma:
                result = ImGuiKey.Comma;
                return true;
            case Key.Period:
                result = ImGuiKey.Period;
                return true;
            case Key.Slash:
                result = ImGuiKey.Slash;
                return true;
            case Key.BackSlash:
            case Key.NonUSBackSlash:
                result = ImGuiKey.Backslash;
                return true;
            default:
                result = ImGuiKey.GamepadBack;
                return false;
        }
    }

    public static bool TryMapKey(SDL_Keycode key, out ImGuiKey result)
    {
        ImGuiKey keyToImGuiKeyShortcut(SDL_Keycode keyToConvert, SDL_Keycode startKey1, ImGuiKey startKey2)
        {
            int changeFromStart1 = (int)keyToConvert - (int)startKey1;
            return startKey2 + changeFromStart1;
        }

        if (key is >= SDL_Keycode.SDLK_F1 and <= SDL_Keycode.SDLK_F12)
        {
            result = keyToImGuiKeyShortcut(key, SDL_Keycode.SDLK_F1, ImGuiKey.F1);
            return true;
        }

        if (key is >= SDL_Keycode.SDLK_KP_1 and <= SDL_Keycode.SDLK_KP_0)
        {
            result = key == SDL_Keycode.SDLK_KP_0 ? ImGuiKey.Keypad0 :
                keyToImGuiKeyShortcut(key, SDL_Keycode.SDLK_KP_1, ImGuiKey.Keypad1);
            return true;
        }

        if (key is >= SDL_Keycode.SDLK_a and <= SDL_Keycode.SDLK_z)
        {
            result = keyToImGuiKeyShortcut(key, SDL_Keycode.SDLK_a, ImGuiKey.A);
            return true;
        }

        if (key is >= SDL_Keycode.SDLK_0 and <= SDL_Keycode.SDLK_9)
        {
            result = keyToImGuiKeyShortcut(key, SDL_Keycode.SDLK_0, ImGuiKey._0);
            return true;
        }

        switch (key)
        {
            case SDL_Keycode.SDLK_LSHIFT:
            case SDL_Keycode.SDLK_RSHIFT:
                result = ImGuiKey.ModShift;
                return true;
            case SDL_Keycode.SDLK_LCTRL:
            case SDL_Keycode.SDLK_RCTRL:
                result = ImGuiKey.ModCtrl;
                return true;
            case SDL_Keycode.SDLK_LALT:
            case SDL_Keycode.SDLK_RALT:
                result = ImGuiKey.ModAlt;
                return true;
            case SDL_Keycode.SDLK_LGUI:
            case SDL_Keycode.SDLK_RGUI:
                result = ImGuiKey.ModSuper;
                return true;
            case SDL_Keycode.SDLK_MENU:
                result = ImGuiKey.Menu;
                return true;
            case SDL_Keycode.SDLK_UP:
                result = ImGuiKey.UpArrow;
                return true;
            case SDL_Keycode.SDLK_DOWN:
                result = ImGuiKey.DownArrow;
                return true;
            case SDL_Keycode.SDLK_LEFT:
                result = ImGuiKey.LeftArrow;
                return true;
            case SDL_Keycode.SDLK_RIGHT:
                result = ImGuiKey.RightArrow;
                return true;
            case SDL_Keycode.SDLK_RETURN:
                result = ImGuiKey.Enter;
                return true;
            case SDL_Keycode.SDLK_ESCAPE:
                result = ImGuiKey.Escape;
                return true;
            case SDL_Keycode.SDLK_SPACE:
                result = ImGuiKey.Space;
                return true;
            case SDL_Keycode.SDLK_TAB:
                result = ImGuiKey.Tab;
                return true;
            case SDL_Keycode.SDLK_BACKSPACE:
                result = ImGuiKey.Backspace;
                return true;
            case SDL_Keycode.SDLK_INSERT:
                result = ImGuiKey.Insert;
                return true;
            case SDL_Keycode.SDLK_DELETE:
                result = ImGuiKey.Delete;
                return true;
            case SDL_Keycode.SDLK_PAGEUP:
                result = ImGuiKey.PageUp;
                return true;
            case SDL_Keycode.SDLK_PAGEDOWN:
                result = ImGuiKey.PageDown;
                return true;
            case SDL_Keycode.SDLK_HOME:
                result = ImGuiKey.Home;
                return true;
            case SDL_Keycode.SDLK_END:
                result = ImGuiKey.End;
                return true;
            case SDL_Keycode.SDLK_CAPSLOCK:
                result = ImGuiKey.CapsLock;
                return true;
            case SDL_Keycode.SDLK_SCROLLLOCK:
                result = ImGuiKey.ScrollLock;
                return true;
            case SDL_Keycode.SDLK_PRINTSCREEN:
                result = ImGuiKey.PrintScreen;
                return true;
            case SDL_Keycode.SDLK_PAUSE:
                result = ImGuiKey.Pause;
                return true;
            case SDL_Keycode.SDLK_NUMLOCKCLEAR:
                result = ImGuiKey.NumLock;
                return true;
            case SDL_Keycode.SDLK_KP_DIVIDE:
                result = ImGuiKey.KeypadDivide;
                return true;
            case SDL_Keycode.SDLK_KP_MULTIPLY:
                result = ImGuiKey.KeypadMultiply;
                return true;
            case SDL_Keycode.SDLK_KP_MINUS:
                result = ImGuiKey.KeypadSubtract;
                return true;
            case SDL_Keycode.SDLK_KP_PLUS:
                result = ImGuiKey.KeypadAdd;
                return true;
            case SDL_Keycode.SDLK_KP_DECIMAL:
                result = ImGuiKey.KeypadDecimal;
                return true;
            case SDL_Keycode.SDLK_KP_ENTER:
                result = ImGuiKey.KeypadEnter;
                return true;
            case SDL_Keycode.SDLK_BACKQUOTE:
                result = ImGuiKey.GraveAccent;
                return true;
            case SDL_Keycode.SDLK_MINUS:
                result = ImGuiKey.Minus;
                return true;
            case SDL_Keycode.SDLK_PLUS:
                result = ImGuiKey.Equal;
                return true;
            case SDL_Keycode.SDLK_LEFTBRACKET:
                result = ImGuiKey.LeftBracket;
                return true;
            case SDL_Keycode.SDLK_RIGHTBRACKET:
                result = ImGuiKey.RightBracket;
                return true;
            case SDL_Keycode.SDLK_SEMICOLON:
                result = ImGuiKey.Semicolon;
                return true;
            case SDL_Keycode.SDLK_QUOTE:
                result = ImGuiKey.Apostrophe;
                return true;
            case SDL_Keycode.SDLK_COMMA:
                result = ImGuiKey.Comma;
                return true;
            case SDL_Keycode.SDLK_PERIOD:
                result = ImGuiKey.Period;
                return true;
            case SDL_Keycode.SDLK_SLASH:
                result = ImGuiKey.Slash;
                return true;
            case SDL_Keycode.SDLK_BACKSLASH:
                result = ImGuiKey.Backslash;
                return true;
            default:
                result = ImGuiKey.GamepadBack;
                return false;
        }
    }

    // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
    public static bool TryMapModifierKey(ModifierKeys key, out ImGuiKey result)
    {
        result = ImGuiKey.ModNone;

        if (key.HasFlag(ModifierKeys.Alt))
            result |= ImGuiKey.ModAlt;
        if (key.HasFlag(ModifierKeys.Control))
            result |= ImGuiKey.ModCtrl;
        if (key.HasFlag(ModifierKeys.Shift))
            result |= ImGuiKey.ModShift;
        if (key.HasFlag(ModifierKeys.Gui))
            result |= ImGuiKey.ModSuper;

        return true;
    }

    public static bool TryMapMouseButton(MouseButton mouse, out ImGuiMouseButton? result)
    {
        result = null;

        if (mouse == MouseButton.Left)
            result = ImGuiMouseButton.Left;
        else if (mouse == MouseButton.Right)
            result = ImGuiMouseButton.Right;
        else if (mouse == MouseButton.Middle)
            result = ImGuiMouseButton.Middle;

        return result.HasValue;
    }
}