namespace WindowsPrinter.Services.Abstractions;

public interface IAppDiagnostics
{
    void LogCrash(string source, Exception ex);

    void LogWarning(string source, string message);
}
