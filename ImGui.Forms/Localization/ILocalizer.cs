using System.Collections.Generic;

namespace ImGui.Forms.Localization
{
    public interface ILocalizer
    {
        string CurrentLocale { get; }

        IList<string> GetLocales();

        string GetLanguageName(string locale);
        void ChangeLocale(string locale);

        string Localize(string localizationId, params object[] args);
    }
}
