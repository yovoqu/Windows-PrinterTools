namespace WindowsPrinter.Services.Abstractions;

public interface IFilePickerService
{
    Task<IReadOnlyList<string>> PickFilesAsync(IReadOnlyList<string> extensions);

    Task<string?> PickFolderAsync();
}
