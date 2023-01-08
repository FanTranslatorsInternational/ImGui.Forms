using System.Collections.Generic;
using ImGui.Forms.Localization;

namespace ImGui.Forms.Modals.IO
{
    public class FileFilter
    {
        public LocalizedString Name { get; }
        public IList<string> Extensions { get; } = new List<string>();

        public FileFilter(LocalizedString name, params string[] extensions)
        {
            Name = name;
            foreach (var ext in extensions)
                Extensions.Add(ext);
        }

        public override string ToString()
        {
            var result = Name;
            if ((Extensions?.Count ?? 0) > 0)
                result += " (" + string.Join(", ", Extensions) + ")";

            return result;
        }
    }
}
