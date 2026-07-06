using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Preferences;

public sealed class UserPreferences
{
    public string? LastPrinterName { get; init; }

    public bool UseColor { get; init; } = true;

    public int Copies { get; init; } = 1;

    public PrintDuplexMode Duplex { get; init; } = PrintDuplexMode.Simplex;

    public int ShellSpoolerTimeoutSeconds { get; init; } = 60;
}
