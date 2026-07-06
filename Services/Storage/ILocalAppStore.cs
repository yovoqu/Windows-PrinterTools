namespace WindowsPrinter.Services.Storage;

/// <summary>
/// File-system backed app data under %LocalAppData%/{appName}/.
/// Works for unpackaged WinUI apps without requiring ApplicationData / package identity.
/// </summary>
public interface ILocalAppStore
{
    string Directory { get; }

    string GetPath(string relativePath);

    T? ReadJson<T>(string relativePath) where T : class;

    void WriteJson<T>(string relativePath, T data);

    void AppendLine(string relativePath, string line);
}
