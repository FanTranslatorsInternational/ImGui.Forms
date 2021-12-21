using System.Collections.Generic;

namespace ImGui.Forms.Controls.Base
{
    public abstract class Container<T> : Component
    {
        public IList<T> Children { get; } = new List<T>();
    }
}
