using System.Collections.Generic;

namespace ImGui.Forms.Localization
{
    public interface ILocalizer
    {
        string CurrentLocale { get; }

        IList<string> GetLocales();

        string GetLanguageName(string locale);
        void ChangeLocale(string locale);

        bool TryLocalize(string localizationId, out string localization, params object[] args);
        string Localize(string localizationId, params object[] args);
    }
}
