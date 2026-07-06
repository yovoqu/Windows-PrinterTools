using WindowsPrinter.Services.Abstractions;
using WindowsPrinter.Services.Storage;

namespace WindowsPrinter.Services;

public sealed class AppDiagnosticsService : IAppDiagnostics
{
    private const string CrashLogFileName = "crash.log";

    private readonly ILocalAppStore _store;

    public AppDiagnosticsService(ILocalAppStore store) => _store = store;

    public void LogCrash(string source, Exception ex) => App.LogCrash(source, ex);

    public void LogWarning(string source, string message)
    {
        try
        {
            _store.AppendLine(CrashLogFileName,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARNING {source}{Environment.NewLine}{message}{Environment.NewLine}");
        }
        catch
        {
            // ignore logging failures
        }
    }
}
