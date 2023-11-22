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

        /// <summary>
        /// Set up a localized string, based on a localization ID.
        /// </summary>
        /// <param name="id">The ID of the localization to return.</param>
        public LocalizedString(string id) : this(id, null, Array.Empty<Func<object>>())
        { }

        /// <summary>
        /// Set up a localized string, based on a localization ID and formatting arguments.
        /// </summary>
        /// <param name="id">The ID of the localization to return.</param>
        /// <param name="args">The arguments of the localization to return.</param>
        public LocalizedString(string id, params Func<object>[] args) : this(id, null, args)
        { }

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

        public override string ToString()
        {
            if (_fixedText != null)
                return _fixedText;

            var app = Application.Instance;
            if (app?.Localizer == null || _id == null || _args == null)
                return string.Empty;

            if (app.Localizer.CurrentLocale == _locale)
                return _localizedText;

            _locale = app.Localizer.CurrentLocale;

            object[] args = _args.Select(x => x?.Invoke() ?? string.Empty).ToArray();
            return _localizedText = app.Localizer.Localize(_id, args);
        }

        public static implicit operator LocalizedString(string s) => new LocalizedString(null, s ?? string.Empty, Array.Empty<Func<object>>());
        public static implicit operator string(LocalizedString s) => s.ToString();
    }
}
