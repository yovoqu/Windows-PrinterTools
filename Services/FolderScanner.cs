using WindowsPrinter.Services.Printing;

namespace WindowsPrinter.Services;

public static class FolderScanner
{
    public static IReadOnlyList<string> ScanSupportedFiles(string folderPath, IPrintHandlerRegistry registry) =>
        ScanSupportedFilesAsync(folderPath, registry).GetAwaiter().GetResult();

    public static Task<IReadOnlyList<string>> ScanSupportedFilesAsync(
        string folderPath,
        IPrintHandlerRegistry registry,
        CancellationToken ct = default) =>
        Task.Run(() => ScanCore(folderPath, registry, ct), ct);

    private static IReadOnlyList<string> ScanCore(
        string folderPath,
        IPrintHandlerRegistry registry,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!Directory.Exists(folderPath))
            return [];

        var results = new List<string>();
        foreach (var path in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            if (registry.IsSupported(Path.GetExtension(path)))
                results.Add(path);
        }

        results.Sort(StringComparer.OrdinalIgnoreCase);
        return results;
    }
}
