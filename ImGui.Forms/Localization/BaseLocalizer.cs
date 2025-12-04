using System.Collections.Generic;
using System.Linq;

namespace ImGui.Forms.Localization;

public abstract class BaseLocalizer : ILocalizer
{
    private IDictionary<string, LanguageInfo> _localizations;

    protected abstract string DefaultLocale { get; }
    protected abstract string UndefinedValue { get; }

    public string CurrentLocale { get; private set; }

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

    public bool TryLocalize(string localizationId, out string localization, params object[] args)
    {
        localization = Localize(localizationId, args);
        return localization != UndefinedValue;
    }

    public string Localize(string localizationId, params object[] args)
    {
        // Return localization of current locale
        if (_localizations.TryGetValue(CurrentLocale, out LanguageInfo language)
            && language.LocalizationEntries.TryGetValue(localizationId, out string localization))
            return string.Format(localization, args);

        // Otherwise, return localization of default locale
        if (_localizations.TryGetValue(DefaultLocale, out language)
            && language.LocalizationEntries.TryGetValue(localizationId, out localization))
            return string.Format(localization, args);

        // Otherwise, return localization placeholder
        return UndefinedValue;
    }

    protected void Initialize()
    {
        _localizations = GetLocalizations();

        // HINT: SetCurrentLocale is not used on purpose, to not apply override logic in case of failure
        CurrentLocale = GetInitialLocale();
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