using System.Collections.Generic;

namespace ImGui.Forms.Modals.IO
{
    public class FileFilter
    {
        public string Name { get; set; }
        public IList<string> Extensions { get; } = new List<string>();

        public override string ToString()
        {
            var result = Name;
            if ((Extensions?.Count ?? 0) > 0)
                result += " (" + string.Join(", ", Extensions) + ")";

            return result;
        }
    }
}
