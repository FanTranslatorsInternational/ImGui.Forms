using System.Collections.Generic;

namespace ImGui.Forms.Components.Base
{
    public abstract class Container<T> : Component
    {
        public IList<T> Children { get; } = new List<T>();
    }
}
