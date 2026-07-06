using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Logging;

public interface IPrintSessionLog
{
    IReadOnlyList<PrintLogEntry> Entries { get; }
    string LogFilePath { get; }
    event EventHandler? Changed;

    void Information(string source, string message, string? fileName = null);
    void Success(string source, string message, string? fileName = null);
    void Warning(string source, string message, string? fileName = null);
    void Error(string source, string message, string? fileName = null);
    void Clear();
}
