namespace ImGui.Forms.Localization
{
    public interface ILocalizer
    {
        string CurrentLocale { get; }

        void ChangeLocale(string locale);

        string Localize(string name, params object[] args);
    }
}
