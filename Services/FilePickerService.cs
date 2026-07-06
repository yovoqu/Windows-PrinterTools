using Windows.Storage.Pickers;
using WindowsPrinter.Services.Abstractions;
using WinRT.Interop;

namespace WindowsPrinter.Services;

public sealed class FilePickerService : IFilePickerService
{
    private readonly IWindowHandleProvider _windowHandle;
    private readonly IAppDiagnostics _diagnostics;

    public FilePickerService(IWindowHandleProvider windowHandle, IAppDiagnostics diagnostics)
    {
        _windowHandle = windowHandle;
        _diagnostics = diagnostics;
    }

    public async Task<IReadOnlyList<string>> PickFilesAsync(IReadOnlyList<string> extensions)
    {
        if (!_windowHandle.IsAvailable)
        {
            _diagnostics.LogWarning(nameof(FilePickerService), "Main window is not available; file picker cannot be shown.");
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

        InitializeWithWindow.Initialize(picker, _windowHandle.GetWindowHandle());

        var files = await picker.PickMultipleFilesAsync();
        if (files is null || files.Count == 0)
        {
            return [];
        }

        return files.Select(f => f.Path).ToList();
    }

    public async Task<string?> PickFolderAsync()
    {
        if (!_windowHandle.IsAvailable)
        {
            _diagnostics.LogWarning(nameof(FilePickerService), "Main window is not available; folder picker cannot be shown.");
            return null;
        }

        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List
        };

        picker.FileTypeFilter.Add("*");

        InitializeWithWindow.Initialize(picker, _windowHandle.GetWindowHandle());

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
