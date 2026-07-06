namespace WindowsPrinter.Services.Preferences;

public interface IUserPreferencesService
{
    UserPreferences Load();

    void Save(UserPreferences preferences);
}
