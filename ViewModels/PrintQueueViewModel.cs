using System.Collections.ObjectModel;
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

    public PrintQueueViewModel(IFilePickerService filePicker)
    {
        _filePicker = filePicker;
        Files.CollectionChanged += (_, _) => RefreshDisplayedFiles();
    }

    public ObservableCollection<PrintFileItem> Files { get; } = [];
    public ObservableCollection<PrintFileItem> DisplayedFiles { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectionSummary = "已选择 0 / 0 个文件";
    [ObservableProperty] private bool _isQueueEmpty = true;

    public bool HasFiles => !IsQueueEmpty;

    public bool IsPrinting { get; set; }

    partial void OnSearchTextChanged(string value) => RefreshDisplayedFiles();

    [RelayCommand(CanExecute = nameof(CanModifyFiles))]
    private async Task AddFilesAsync()
    {
        var paths = await _filePicker.PickFilesAsync(PrintHandlerFactory.SupportedExtensions.ToList());
        if (paths.Count == 0) return;
        QueueChanged?.Invoke(this, new QueueChangedEventArgs(AddPathsToQueue(paths), null));
    }

    [RelayCommand(CanExecute = nameof(CanModifyFiles))]
    private async Task AddFolderAsync()
    {
        var folderPath = await _filePicker.PickFolderAsync();
        if (string.IsNullOrWhiteSpace(folderPath)) return;

        var paths = FolderScanner.ScanSupportedFiles(folderPath);
        if (paths.Count == 0)
        {
            QueueChanged?.Invoke(this, new QueueChangedEventArgs(0, "所选文件夹中没有可打印的文件。"));
            return;
        }

        var added = AddPathsToQueue(paths, folderPath);
        var skipped = paths.Count - added;
        string? message = added == 0
            ? "所选文件夹中的文件均已在队列中。"
            : skipped > 0
                ? $"已从文件夹添加 {added} 个文件，跳过 {skipped} 个重复项（共扫描 {paths.Count} 个）。"
                : $"已从文件夹添加 {added} 个文件（共扫描 {paths.Count} 个）。";

        QueueChanged?.Invoke(this, new QueueChangedEventArgs(added, message));
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
        foreach (var file in toRemove) Files.Remove(file);
        UpdateSelectionSummary();
        QueueChanged?.Invoke(this, new QueueChangedEventArgs(-toRemove.Count, $"已移除 {toRemove.Count} 个文件。"));
    }

    public void NotifyPrintingStateChanged()
    {
        AddFilesCommand.NotifyCanExecuteChanged();
        AddFolderCommand.NotifyCanExecuteChanged();
        SelectAllCommand.NotifyCanExecuteChanged();
        ClearSelectionCommand.NotifyCanExecuteChanged();
        RemoveSelectedCommand.NotifyCanExecuteChanged();
    }

    public event EventHandler<QueueChangedEventArgs>? QueueChanged;
    public event EventHandler? SelectionChanged;

    private int AddPathsToQueue(IEnumerable<string> paths, string? folderRoot = null)
    {
        var added = 0;
        var skipped = new List<string>();

        foreach (var path in paths)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (!PrintHandlerFactory.SupportedExtensions.Contains(extension))
            {
                skipped.Add(Path.GetFileName(path));
                continue;
            }

            if (Files.Any(f => string.Equals(f.FilePath, path, StringComparison.OrdinalIgnoreCase)))
                continue;

            PrintFileItem item;
            try
            {
                item = new PrintFileItem(path, folderRoot);
            }
            catch
            {
                continue;
            }

            item.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(PrintFileItem.IsSelected) or nameof(PrintFileItem.Status))
                {
                    UpdateSelectionSummary();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            Files.Add(item);
            added++;
        }

        UpdateSelectionSummary();
        if (skipped.Count > 0)
            QueueChanged?.Invoke(this, new QueueChangedEventArgs(added, $"以下文件格式暂不支持：{string.Join("、", skipped)}"));

        return added;
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

        IsQueueEmpty = Files.Count == 0;
        OnPropertyChanged(nameof(HasFiles));
    }

    private void UpdateSelectionSummary()
    {
        SelectionSummary = $"已选择 {Files.Count(f => f.IsSelected)} / {Files.Count} 个文件";
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool CanModifyFiles() => !IsPrinting;
}

public sealed class QueueChangedEventArgs(int count, string? message) : EventArgs
{
    public int Count { get; } = count;
    public string? Message { get; } = message;
}
