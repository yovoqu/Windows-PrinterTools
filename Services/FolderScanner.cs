using WindowsPrinter.Services.Printing;

namespace WindowsPrinter.Services;

public static class FolderScanner
{
    public static IReadOnlyList<string> ScanSupportedFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(path => PrintHandlerFactory.SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
