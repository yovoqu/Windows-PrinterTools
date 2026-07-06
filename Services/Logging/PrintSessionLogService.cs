using WindowsPrinter.Models;
using WindowsPrinter.Services.Storage;

namespace WindowsPrinter.Services.Logging;

public sealed class PrintSessionLogService : IPrintSessionLog
{
    private const int MaxEntries = 500;
    private const string LogFileName = "session.log";

    private readonly ILocalAppStore _store;
    private readonly object _sync = new();
    private readonly List<PrintLogEntry> _entries = [];

    public PrintSessionLogService(ILocalAppStore store) => _store = store;

    public IReadOnlyList<PrintLogEntry> Entries
    {
        get
        {
            lock (_sync)
                return _entries;
        }
    }

    public string LogFilePath => _store.GetPath(LogFileName);

    public event EventHandler? Changed;

    public void Information(string source, string message, string? fileName = null) =>
        Add(PrintLogLevel.Information, source, message, fileName);

    public void Success(string source, string message, string? fileName = null) =>
        Add(PrintLogLevel.Success, source, message, fileName);

    public void Warning(string source, string message, string? fileName = null) =>
        Add(PrintLogLevel.Warning, source, message, fileName);

    public void Error(string source, string message, string? fileName = null) =>
        Add(PrintLogLevel.Error, source, message, fileName);

    public void Clear()
    {
        lock (_sync)
            _entries.Clear();

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Add(PrintLogLevel level, string source, string message, string? fileName)
    {
        var entry = new PrintLogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Source = source,
            Message = message,
            FileName = fileName
        };

        lock (_sync)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries)
                _entries.RemoveRange(0, _entries.Count - MaxEntries);
        }

        AppendToFile(entry);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void AppendToFile(PrintLogEntry entry)
    {
        try
        {
            var line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.LevelText}] [{entry.Source}] {entry.Message}";
            if (!string.IsNullOrWhiteSpace(entry.FileName))
                line += $" ({entry.FileName})";
            _store.AppendLine(LogFileName, line);
        }
        catch
        {
            // ignore file logging failures
        }
    }
}
