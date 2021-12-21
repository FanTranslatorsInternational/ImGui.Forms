namespace ImGui.Forms.Localization
{
    public interface ILocalizer
    {
        void ChangeLocale(string locale);

        string Localize(string name, params object[] args);
    }
}
