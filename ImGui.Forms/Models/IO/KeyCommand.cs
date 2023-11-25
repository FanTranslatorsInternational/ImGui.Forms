using System;
using Veldrid;

namespace ImGui.Forms.Models.IO
{
    public struct KeyCommand
    {
        public ModifierKeys modifiers;
        public Key key;

        public bool IsEmpty => modifiers == 0 && key == 0;

        public KeyCommand(Key key) : this(ModifierKeys.None, key)
        { }

        public KeyCommand(ModifierKeys modifiers, Key key)
        {
            this.modifiers = modifiers;
            this.key = key;
        }

        public static bool operator ==(KeyCommand a, KeyCommand b) => a.modifiers == b.modifiers && a.key == b.key;
        public static bool operator !=(KeyCommand a, KeyCommand b) => a.modifiers != b.modifiers || a.key != b.key;
    }
}
