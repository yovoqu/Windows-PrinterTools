using WindowsPrinter.Models;
using WindowsPrinter.Services.Storage;

namespace WindowsPrinter.Services.Preferences;

public sealed class UserPreferencesService : IUserPreferencesService
{
    private const string PreferencesFile = "preferences.json";

    private readonly ILocalAppStore _store;
    private readonly object _sync = new();

    public UserPreferencesService(ILocalAppStore store) => _store = store;

    public UserPreferences Load()
    {
        lock (_sync)
        {
            var stored = _store.ReadJson<UserPreferences>(PreferencesFile);
            if (stored is null)
                return new UserPreferences();

            return new UserPreferences
            {
                LastPrinterName = stored.LastPrinterName,
                UseColor = stored.UseColor,
                Copies = Math.Clamp(stored.Copies, 1, 99),
                Duplex = Enum.IsDefined(typeof(PrintDuplexMode), stored.Duplex)
                    ? stored.Duplex
                    : PrintDuplexMode.Simplex,
                ShellSpoolerTimeoutSeconds = Math.Clamp(stored.ShellSpoolerTimeoutSeconds, 15, 300)
            };
        }
    }

    public void Save(UserPreferences preferences)
    {
        var normalized = new UserPreferences
        {
            LastPrinterName = string.IsNullOrWhiteSpace(preferences.LastPrinterName)
                ? null
                : preferences.LastPrinterName,
            UseColor = preferences.UseColor,
            Copies = Math.Clamp(preferences.Copies, 1, 99),
            Duplex = preferences.Duplex,
            ShellSpoolerTimeoutSeconds = Math.Clamp(preferences.ShellSpoolerTimeoutSeconds, 15, 300)
        };

        lock (_sync)
            _store.WriteJson(PreferencesFile, normalized);
    }
}
