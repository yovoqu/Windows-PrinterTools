using System.Text.Json;

namespace WindowsPrinter.Services.Storage;

public sealed class LocalAppStore : ILocalAppStore
{
    public const string AppFolderName = "WindowsPrinter";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly object _sync = new();

    public LocalAppStore()
        : this(null)
    {
    }

    internal LocalAppStore(string? directoryOverride)
    {
        Directory = directoryOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName);
        System.IO.Directory.CreateDirectory(Directory);
    }

    public string Directory { get; }

    public string GetPath(string relativePath) => Path.Combine(Directory, relativePath);

    public T? ReadJson<T>(string relativePath) where T : class
    {
        var path = GetPath(relativePath);
        if (!File.Exists(path))
            return null;

        lock (_sync)
        {
            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }
    }

    public void WriteJson<T>(string relativePath, T data)
    {
        var path = GetPath(relativePath);
        lock (_sync)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(path, json);
        }
    }

    public void AppendLine(string relativePath, string line)
    {
        var path = GetPath(relativePath);
        lock (_sync)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}
