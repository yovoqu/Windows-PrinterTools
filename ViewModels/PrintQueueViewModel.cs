using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowsPrinter.Models;
using WindowsPrinter.Services;
using WindowsPrinter.Services.Abstractions;
using WindowsPrinter.Services.Printing;

namespace WindowsPrinter.ViewModels;

public sealed partial class PrintQueueViewModel : ObservableObject
{
    private readonly IFilePickerService _filePicker;
    private readonly IPrintFileItemFactory _fileFactory;
    private readonly IPrintHandlerRegistry _registry;
    private readonly IAppDiagnostics _diagnostics;
    private readonly Dictionary<PrintFileItem, PropertyChangedEventHandler> _itemHandlers = new();

    public PrintQueueViewModel(
        IFilePickerService filePicker,
        IPrintFileItemFactory fileFactory,
        IPrintHandlerRegistry registry,
        IAppDiagnostics diagnostics)
    {
        _filePicker = filePicker;
        _fileFactory = fileFactory;
        _registry = registry;
        _diagnostics = diagnostics;
        Files.CollectionChanged += (_, _) => RefreshDisplayedFiles();
    }

    public ObservableCollection<PrintFileItem> Files { get; } = [];
    public ObservableCollection<PrintFileItem> DisplayedFiles { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectionSummary = "已选择 0 / 0 个文件";
    [ObservableProperty] private bool _isQueueEmpty = true;
    [ObservableProperty] private bool _isPrinting;

    public bool HasFiles => !IsQueueEmpty;

    partial void OnIsPrintingChanged(bool value) => NotifyPrintingStateChanged();

    partial void OnSearchTextChanged(string value) => RefreshDisplayedFiles();

    [RelayCommand(CanExecute = nameof(CanModifyFiles))]
    private async Task AddFilesAsync()
    {
        var paths = await _filePicker.PickFilesAsync(_registry.SupportedExtensions.OrderBy(e => e).ToList());
        if (paths.Count == 0) return;

        var result = AddPaths(paths);
        var message = BuildAddPathsMessage(result);
        QueueChanged?.Invoke(this, new QueueChangedEventArgs(result.AddedCount, message));
    }

    [RelayCommand(CanExecute = nameof(CanModifyFiles))]
    private async Task AddFolderAsync()
    {
        var folderPath = await _filePicker.PickFolderAsync();
        if (string.IsNullOrWhiteSpace(folderPath)) return;

        var paths = await FolderScanner.ScanSupportedFilesAsync(folderPath, _registry);
        if (paths.Count == 0)
        {
            QueueChanged?.Invoke(this, new QueueChangedEventArgs(0, "所选文件夹中没有可打印的文件。"));
            return;
        }

        var result = AddPaths(paths, folderPath);
        var message = BuildAddPathsMessage(result, scannedCount: paths.Count, fromFolder: true);
        QueueChanged?.Invoke(this, new QueueChangedEventArgs(result.AddedCount, message));
    }

    [RelayCommand(CanExecute = nameof(CanModifyFiles))]
    private void SelectAll()
    {
        foreach (var file in Files) file.IsSelected = true;
        UpdateSelectionSummary();
    }

    [RelayCommand(CanExecute = nameof(CanModifyFiles))]
    private void ClearSelection()
    {
        foreach (var file in Files) file.IsSelected = false;
        UpdateSelectionSummary();
    }

    [RelayCommand(CanExecute = nameof(CanModifyFiles))]
    private void RemoveSelected()
    {
        var toRemove = Files.Where(f => f.IsSelected).ToList();
        foreach (var file in toRemove)
        {
            UnsubscribeItem(file);
            Files.Remove(file);
        }

        UpdateSelectionSummary();
        QueueChanged?.Invoke(this, new QueueChangedEventArgs(-toRemove.Count, $"已移除 {toRemove.Count} 个文件。"));
    }

    public void ReportOperationError(string message) =>
        QueueChanged?.Invoke(this, new QueueChangedEventArgs(0, message));

    [RelayCommand(CanExecute = nameof(CanClearQueue))]
    private void ClearQueue()
    {
        var count = Files.Count;
        foreach (var file in Files.ToList())
        {
            UnsubscribeItem(file);
            Files.Remove(file);
        }

        UpdateSelectionSummary();
        QueueChanged?.Invoke(this, new QueueChangedEventArgs(-count, "已清空打印队列。"));
    }

    public async Task<int> AddDroppedPathsAsync(IEnumerable<string> paths)
    {
        if (IsPrinting)
        {
            ReportOperationError("打印进行中，无法拖放添加文件。");
            return 0;
        }

        var filePaths = await CollectDroppedPathsAsync(paths);
        if (filePaths.Count == 0)
        {
            QueueChanged?.Invoke(this, new QueueChangedEventArgs(0, "拖入的内容中没有可打印的文件。"));
            return 0;
        }

        var result = AddPaths(filePaths);
        var message = BuildAddPathsMessage(result, fromDrop: true);
        if (!string.IsNullOrWhiteSpace(message))
            QueueChanged?.Invoke(this, new QueueChangedEventArgs(result.AddedCount, message));

        return result.AddedCount;
    }

    public AddPathsResult AddPaths(IEnumerable<string> paths, string? folderRoot = null)
    {
        var unsupported = new List<string>();
        var failed = new List<AddPathFailure>();
        var duplicateCount = 0;
        var added = 0;

        foreach (var path in paths)
        {
            if (!_registry.IsSupported(Path.GetExtension(path)))
            {
                unsupported.Add(Path.GetFileName(path));
                continue;
            }

            if (Files.Any(f => string.Equals(f.FilePath, path, StringComparison.OrdinalIgnoreCase)))
            {
                duplicateCount++;
                continue;
            }

            PrintFileItem item;
            try
            {
                item = _fileFactory.Create(path, folderRoot);
            }
            catch (Exception ex) when (ex is NotSupportedException
                or FileNotFoundException
                or DirectoryNotFoundException
                or UnauthorizedAccessException
                or IOException
                or ArgumentException)
            {
                failed.Add(new AddPathFailure(path, ex.Message));
                _diagnostics.LogWarning("AddPaths", $"{path}: {ex.Message}");
                continue;
            }

            SubscribeItem(item);
            Files.Add(item);
            added++;
        }

        UpdateSelectionSummary();
        return new AddPathsResult
        {
            AddedCount = added,
            DuplicateCount = duplicateCount,
            UnsupportedFiles = unsupported,
            FailedFiles = failed
        };
    }

    public void NotifyPrintingStateChanged()
    {
        AddFilesCommand.NotifyCanExecuteChanged();
        AddFolderCommand.NotifyCanExecuteChanged();
        SelectAllCommand.NotifyCanExecuteChanged();
        ClearSelectionCommand.NotifyCanExecuteChanged();
        RemoveSelectedCommand.NotifyCanExecuteChanged();
        ClearQueueCommand.NotifyCanExecuteChanged();
    }

    public event EventHandler<QueueChangedEventArgs>? QueueChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler? FileStatusChanged;

    private async Task<List<string>> CollectDroppedPathsAsync(IEnumerable<string> paths)
    {
        var filePaths = new List<string>();
        foreach (var path in paths)
        {
            if (File.Exists(path))
                filePaths.Add(path);
            else if (Directory.Exists(path))
                filePaths.AddRange(await FolderScanner.ScanSupportedFilesAsync(path, _registry));
        }

        return filePaths;
    }

    private static string? BuildAddPathsMessage(
        AddPathsResult result,
        int? scannedCount = null,
        bool fromFolder = false,
        bool fromDrop = false)
    {
        if (result.AddedCount == 0
            && result.DuplicateCount == 0
            && result.UnsupportedFiles.Count == 0
            && result.FailedFiles.Count == 0)
        {
            return null;
        }

        var parts = new List<string>();

        if (result.AddedCount > 0)
        {
            if (fromFolder && scannedCount.HasValue)
                parts.Add($"已从文件夹添加 {result.AddedCount} 个文件（共扫描 {scannedCount} 个）");
            else if (fromDrop)
                parts.Add($"已从拖放添加 {result.AddedCount} 个文件");
            else
                parts.Add($"已添加 {result.AddedCount} 个文件");
        }
        else if (fromFolder && result.DuplicateCount > 0 && scannedCount.HasValue)
        {
            return "所选文件夹中的文件均已在队列中。";
        }

        if (result.DuplicateCount > 0)
            parts.Add(result.AddedCount > 0
                ? $"跳过 {result.DuplicateCount} 个重复项"
                : $"所选文件均已在队列中（{result.DuplicateCount} 个重复项）");

        if (result.UnsupportedFiles.Count > 0)
            parts.Add($"以下文件格式暂不支持：{string.Join("、", result.UnsupportedFiles)}");

        if (result.FailedFiles.Count > 0)
        {
            var names = result.FailedFiles
                .Select(f => $"{Path.GetFileName(f.Path)}（{f.Reason}）");
            parts.Add($"添加失败 {result.FailedFiles.Count} 个：{string.Join("、", names)}");
        }

        if (parts.Count == 0)
            return result.AddedCount == 0 ? "未能添加任何文件。" : null;

        return string.Join("；", parts);
    }

    private void SubscribeItem(PrintFileItem item)
    {
        PropertyChangedEventHandler handler = (_, args) =>
        {
            if (args.PropertyName is nameof(PrintFileItem.IsSelected) or nameof(PrintFileItem.Status))
            {
                UpdateSelectionSummary();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                if (args.PropertyName == nameof(PrintFileItem.Status))
                    FileStatusChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        item.PropertyChanged += handler;
        _itemHandlers[item] = handler;
    }

    private void UnsubscribeItem(PrintFileItem item)
    {
        if (_itemHandlers.Remove(item, out var handler))
            item.PropertyChanged -= handler;
    }

    private void RefreshDisplayedFiles()
    {
        DisplayedFiles.Clear();
        var query = string.IsNullOrWhiteSpace(SearchText)
            ? Files
            : Files.Where(f =>
                f.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || f.DisplayPath.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var file in query)
            DisplayedFiles.Add(file);

        var wasEmpty = IsQueueEmpty;
        IsQueueEmpty = Files.Count == 0;
        if (wasEmpty != IsQueueEmpty)
        {
            OnPropertyChanged(nameof(HasFiles));
            ClearQueueCommand.NotifyCanExecuteChanged();
        }
    }

    private void UpdateSelectionSummary()
    {
        SelectionSummary = $"已选择 {Files.Count(f => f.IsSelected)} / {Files.Count} 个文件";
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        ClearQueueCommand.NotifyCanExecuteChanged();
    }

    private bool CanModifyFiles() => !IsPrinting;

    private bool CanClearQueue() => !IsPrinting && HasFiles;
}

public sealed class QueueChangedEventArgs(int count, string? message) : EventArgs
{
    public int Count { get; } = count;
    public string? Message { get; } = message;
}
