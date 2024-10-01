using System.Collections.Generic;
using System.Linq;

namespace ImGui.Forms.Localization
{
    public abstract class BaseLocalizer : ILocalizer
    {
        private readonly IDictionary<string, LanguageInfo> _localizations;

        protected abstract string DefaultLocale { get; }
        protected abstract string UndefinedValue { get; }

        public string CurrentLocale { get; private set; }

        public BaseLocalizer()
        {
            _localizations = GetLocalizations();
            
            // HINT: SetCurrentLocale is not used on purpose, to not apply override logic in case of failure
            CurrentLocale = GetInitialLocale();
        }

        public IList<string> GetLocales()
        {
            return _localizations.Keys.ToArray();
        }

        public string GetLanguageName(string locale)
        {
            if (!_localizations.TryGetValue(locale, out LanguageInfo language))
                return UndefinedValue;

            return language.LanguageName;
        }

        public void ChangeLocale(string locale)
        {
            // Do nothing, if locale was not found
            if (!_localizations.ContainsKey(locale))
                return;
            
            SetCurrentLocale(locale);
        }

        public string Localize(string name, params object[] args)
        {
            // Return localization of current locale
            if (_localizations.TryGetValue(CurrentLocale, out LanguageInfo language)
                && language.LocalizationEntries.TryGetValue(name, out string localization))
                return string.Format(localization, args);

            // Otherwise, return localization of default locale
            if (_localizations.TryGetValue(DefaultLocale, out language)
                && language.LocalizationEntries.TryGetValue(name, out localization))
                return string.Format(localization, args);

            // Otherwise, return localization placeholder
            return UndefinedValue;
        }

        protected abstract IList<LanguageInfo> InitializeLocalizations();
        protected abstract string InitializeLocale();

        protected virtual void SetCurrentLocale(string locale)
        {
            CurrentLocale = locale;
        }

        private IDictionary<string, LanguageInfo> GetLocalizations()
        {
            return InitializeLocalizations().ToDictionary(x => x.Locale, y => y);
        }

        private string GetInitialLocale()
        {
            string locale = InitializeLocale();

            if (_localizations.ContainsKey(locale))
                return locale;

            if (_localizations.ContainsKey(DefaultLocale))
                return DefaultLocale;

            return _localizations.FirstOrDefault().Key;
        }
    }

    public record LanguageInfo(string Locale, string LanguageName, IDictionary<string, string> LocalizationEntries);
}
