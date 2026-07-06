using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using WindowsPrinter.Models;
using WindowsPrinter.Services.Abstractions;
using WindowsPrinter.Services.Logging;
using WindowsPrinter.Services.Printing.Preview;

namespace WindowsPrinter.ViewModels;

public sealed partial class PrintWorkspaceViewModel : ObservableObject
{
    private readonly IPrintQueueService _printQueue;
    private readonly IPrintPreviewService _previewService;
    private readonly IPrintSessionLog _log;
    private CancellationTokenSource? _printCts;

    public PrintWorkspaceViewModel(
        PrintQueueViewModel queue,
        PrintSettingsViewModel settings,
        PrintLogViewModel log,
        IPrintQueueService printQueue,
        IPrintPreviewService previewService,
        IPrintSessionLog sessionLog)
    {
        Queue = queue;
        Settings = settings;
        Log = log;
        _printQueue = printQueue;
        _previewService = previewService;
        _log = sessionLog;

        Queue.QueueChanged += OnQueueChanged;
        Queue.SelectionChanged += OnQueueSelectionChanged;
        Queue.FileStatusChanged += (_, _) => RetryFailedCommand.NotifyCanExecuteChanged();
        Settings.PrinterSelectionChanged += (_, _) => NotifyPrintCommandsChanged();
    }

    public PrintQueueViewModel Queue { get; }
    public PrintSettingsViewModel Settings { get; }
    public PrintLogViewModel Log { get; }

    [ObservableProperty] private bool _isPrinting;
    [ObservableProperty] private string _statusMessage = "准备就绪";
    [ObservableProperty] private AppStatusSeverity _statusSeverity = AppStatusSeverity.Informational;
    [ObservableProperty] private bool _isStatusOpen;
    [ObservableProperty] private int _printProgressCurrent;
    [ObservableProperty] private int _printProgressTotal;
    [ObservableProperty] private bool _isPreviewOpen;
    [ObservableProperty] private ImageSource? _previewImageSource;
    [ObservableProperty] private string? _previewCaption;

    public bool ShowPrintProgress => IsPrinting && PrintProgressTotal > 0;

    public double PrintProgressPercent =>
        PrintProgressTotal > 0 ? (double)PrintProgressCurrent / PrintProgressTotal * 100 : 0;

    public bool CanStartPrint =>
        !IsPrinting
        && Queue.Files.Any(f => f.IsSelected)
        && !string.IsNullOrWhiteSpace(Settings.SelectedPrinterName);

    public bool CanPreview =>
        !IsPrinting && Queue.Files.Any(f => f.IsSelected);

    public bool CanRetryFailed =>
        !IsPrinting && Queue.Files.Any(f => f.Status == PrintJobStatus.Failed);

    partial void OnIsPrintingChanged(bool value)
    {
        Queue.IsPrinting = value;
        OnPropertyChanged(nameof(ShowPrintProgress));
        NotifyPrintCommandsChanged();
    }

    partial void OnPrintProgressCurrentChanged(int value)
    {
        OnPropertyChanged(nameof(PrintProgressPercent));
        OnPropertyChanged(nameof(ShowPrintProgress));
    }

    partial void OnPrintProgressTotalChanged(int value)
    {
        OnPropertyChanged(nameof(PrintProgressPercent));
        OnPropertyChanged(nameof(ShowPrintProgress));
    }

    partial void OnIsPreviewOpenChanged(bool value)
    {
        if (!value)
            PreviewImageSource = null;
    }

    [RelayCommand(CanExecute = nameof(CanStartPrint))]
    private async Task StartPrintAsync()
    {
        var selected = Queue.Files.Where(f => f.IsSelected).ToList();
        await RunPrintBatchAsync(selected, "开始打印");
    }

    [RelayCommand(CanExecute = nameof(CanRetryFailed))]
    private async Task RetryFailedAsync()
    {
        var failed = Queue.Files.Where(f => f.Status == PrintJobStatus.Failed).ToList();
        if (failed.Count == 0) return;

        foreach (var file in failed)
        {
            file.IsSelected = true;
            file.ResetStatus();
        }

        await RunPrintBatchAsync(failed, "重试失败项");
    }

    [RelayCommand(CanExecute = nameof(IsPrinting))]
    private void CancelPrint()
    {
        _printCts?.Cancel();
        _log.Warning("Workspace", "用户请求取消打印队列。");
        SetStatus("正在取消打印队列…", AppStatusSeverity.Warning);
    }

    [RelayCommand]
    private void ClosePreview() => IsPreviewOpen = false;

    [RelayCommand(CanExecute = nameof(CanPreview))]
    private async Task PreviewSelectedAsync()
    {
        var file = Queue.Files.FirstOrDefault(f => f.IsSelected);
        if (file is null) return;

        try
        {
            var preview = await _previewService.RenderPreviewAsync(file.FilePath);
            if (preview is null)
            {
                SetStatus($"“{file.FileName}” 不支持预览（通常为系统关联打印的 Office / WebP 格式）。", AppStatusSeverity.Warning);
                return;
            }

            PreviewImageSource = await DecodePngAsync(preview.PngBytes);
            PreviewCaption = preview.Caption;
            IsPreviewOpen = true;
        }
        catch (Exception ex)
        {
            SetStatus($"预览失败：{ex.Message}", AppStatusSeverity.Error);
        }
    }

    private async Task RunPrintBatchAsync(IReadOnlyList<PrintFileItem> files, string batchLabel)
    {
        if (files.Count == 0 || string.IsNullOrWhiteSpace(Settings.SelectedPrinterName)) return;

        PrintSettings printSettings;
        try
        {
            printSettings = Settings.BuildPrintSettings();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, AppStatusSeverity.Error);
            return;
        }

        foreach (var file in files) file.ResetStatus();

        IsPrinting = true;
        PrintProgressTotal = files.Count;
        PrintProgressCurrent = 0;
        _printCts = new CancellationTokenSource();

        _log.Information("Workspace", $"{batchLabel}：共 {files.Count} 个文件。");

        try
        {
            var progress = new Progress<(PrintFileItem file, int current, int total)>(r =>
            {
                PrintProgressCurrent = r.current;
                PrintProgressTotal = r.total;
                SetStatus($"正在打印 ({r.current}/{r.total})：{r.file.FileName}", AppStatusSeverity.Informational);
            });

            var result = await _printQueue.ProcessAsync(files, printSettings, progress, _printCts.Token);

            if (result.Failed == 0)
            {
                _log.Success("Workspace", $"{batchLabel}完成，共 {result.Succeeded} 个文件。");
                SetStatus($"打印完成，共 {result.Succeeded} 个文件。", AppStatusSeverity.Success);
            }
            else
            {
                _log.Warning("Workspace",
                    $"{batchLabel}完成 {result.Succeeded} 个，失败 {result.Failed} 个：{string.Join("、", result.Failures.Select(f => f.File.FileName))}");
                SetStatus(
                    $"完成 {result.Succeeded} 个，失败 {result.Failed} 个：{string.Join("、", result.Failures.Select(f => f.File.FileName))}",
                    AppStatusSeverity.Warning);
            }
        }
        catch (OperationCanceledException)
        {
            _log.Warning("Workspace", $"{batchLabel}已取消。");
            SetStatus("打印队列已取消。", AppStatusSeverity.Warning);
        }
        catch (Exception ex)
        {
            _log.Error("Workspace", $"{batchLabel}失败：{ex.Message}");
            SetStatus($"打印失败：{ex.Message}", AppStatusSeverity.Error);
        }
        finally
        {
            IsPrinting = false;
            PrintProgressCurrent = 0;
            PrintProgressTotal = 0;
            _printCts?.Dispose();
            _printCts = null;
            RetryFailedCommand.NotifyCanExecuteChanged();
        }
    }

    private static async Task<ImageSource> DecodePngAsync(byte[] pngBytes)
    {
        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(pngBytes.AsBuffer());
        stream.Seek(0);

        var image = new BitmapImage();
        await image.SetSourceAsync(stream);
        return image;
    }

    private void NotifyPrintCommandsChanged()
    {
        StartPrintCommand.NotifyCanExecuteChanged();
        CancelPrintCommand.NotifyCanExecuteChanged();
        PreviewSelectedCommand.NotifyCanExecuteChanged();
        RetryFailedCommand.NotifyCanExecuteChanged();
    }

    private void OnQueueSelectionChanged(object? sender, EventArgs e) => NotifyPrintCommandsChanged();

    private void OnQueueChanged(object? sender, QueueChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Message))
        {
            var severity = e.Count > 0 ? AppStatusSeverity.Success
                : e.Count < 0 ? AppStatusSeverity.Informational
                : AppStatusSeverity.Warning;
            SetStatus(e.Message, severity);
        }

        NotifyPrintCommandsChanged();
    }

    private void SetStatus(string message, AppStatusSeverity severity)
    {
        StatusMessage = message;
        StatusSeverity = severity;
        IsStatusOpen = !string.IsNullOrWhiteSpace(message);
    }
}
