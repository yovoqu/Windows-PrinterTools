using Windows.Storage.Pickers;
using WindowsPrinter.Services.Abstractions;
using WinRT.Interop;

namespace WindowsPrinter.Services;

public sealed class FilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<string>> PickFilesAsync(IReadOnlyList<string> extensions)
    {
        var window = App.MainWindow;
        if (window is null)
        {
            return [];
        }

        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List
        };

        foreach (var extension in extensions.OrderBy(e => e))
        {
            picker.FileTypeFilter.Add(extension);
        }

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

        var files = await picker.PickMultipleFilesAsync();
        if (files is null || files.Count == 0)
        {
            return [];
        }

        return files.Select(f => f.Path).ToList();
    }

    public async Task<string?> PickFolderAsync()
    {
        var window = App.MainWindow;
        if (window is null)
        {
            return null;
        }

        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List
        };

        picker.FileTypeFilter.Add("*");

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
