using System;
using System.Linq;

namespace ImGui.Forms.Localization
{
    /// <summary>
    /// A localized string, that always returns either a fixed string, a string from the <see cref="ILocalizer"/> formatted by arguments, or <see cref="string.Empty"/>.
    /// </summary>
    /// <remarks>This will never return <c>null</c>.</remarks>
    public struct LocalizedString
    {
        private readonly string _id;
        private readonly Func<object>[] _args;

        private readonly string _fixedText;

        private string _locale;
        private string _localizedText;

        /// <summary>
        /// Determines, if this localized string has localization information set.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(_fixedText) && (string.IsNullOrEmpty(_id) || _args == null);

        private LocalizedString(string id, string fixedText, Func<object>[] args)
        {
            _locale = null;
            _localizedText = null;

            _args = args;

            if (fixedText != null)
            {
                _id = null;
                _fixedText = fixedText;

                return;
            }

            _id = id ?? throw new ArgumentNullException(nameof(id));
            _fixedText = null;
        }

        /// <summary>
        /// Set up a localized string, based on a localization ID.
        /// </summary>
        /// <param name="localizationId">The ID of the localization to represent.</param>
        public static LocalizedString FromId(string localizationId)
        {
            return new LocalizedString(localizationId, null, Array.Empty<Func<object>>());
        }

        /// <summary>
        /// Set up a localized string, based on a localization ID and formatting arguments.
        /// </summary>
        /// <param name="localizationId">The ID of the localization to represent.</param>
        /// <param name="args">The arguments of the localization to represent.</param>
        public static LocalizedString FromId(string localizationId, params Func<object>[] args)
        {
            return new LocalizedString(localizationId, null, args);
        }

        /// <summary>
        /// Set up a localized string, based on a fixed text.
        /// </summary>
        /// <param name="fixedText">The fixed text to represent.</param>
        public static LocalizedString FromText(string fixedText)
        {
            return new LocalizedString(null, fixedText ?? string.Empty, Array.Empty<Func<object>>());
        }

        public override string ToString()
        {
            if (_fixedText != null)
                return _fixedText;

            var app = Application.Instance;
            if (app?.Localizer == null || _id == null || _args == null)
                return string.Empty;

            if (app.Localizer.CurrentLocale == _locale && _args.Length <= 0)
                return _localizedText;

            _locale = app.Localizer.CurrentLocale;

            object[] args = _args.Select(x => x?.Invoke() ?? string.Empty).ToArray();
            return _localizedText = app.Localizer.Localize(_id, args);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LocalizedString locStr))
                return base.Equals(obj);

            // Both need to have a fixed text set to be potentially equal
            if (_fixedText != null)
            {
                if (locStr._fixedText != null)
                    return _fixedText == locStr._fixedText;

                return false;
            }

            // If one has fixed text and not the other, they can't be equal
            if (locStr._fixedText != null)
                return false;

            return _id == locStr._id;
        }

        public static implicit operator LocalizedString(string s) => FromText(s);
        public static implicit operator string(LocalizedString s) => s.ToString();
    }
}
