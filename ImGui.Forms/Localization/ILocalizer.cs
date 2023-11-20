using System.Collections.Generic;

namespace ImGui.Forms.Localization
{
    public interface ILocalizer
    {
        string CurrentLocale { get; }

        IList<string> GetLocales();

        string GetLanguageName(string locale);
        string GetLocaleByName(string name);
        void ChangeLocale(string locale);

        string Localize(string name, params object[] args);
    }
}
