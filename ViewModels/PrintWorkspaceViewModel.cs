using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowsPrinter.Models;
using WindowsPrinter.Services.Abstractions;

namespace WindowsPrinter.ViewModels;

public sealed partial class PrintWorkspaceViewModel : ObservableObject
{
    private readonly IPrintQueueService _printQueue;
    private CancellationTokenSource? _printCts;

    public PrintWorkspaceViewModel(
        PrintQueueViewModel queue,
        PrintSettingsViewModel settings,
        IPrintQueueService printQueue)
    {
        Queue = queue;
        Settings = settings;
        _printQueue = printQueue;

        Queue.QueueChanged += OnQueueChanged;
        Queue.SelectionChanged += (_, _) => StartPrintCommand.NotifyCanExecuteChanged();
        Settings.PrinterSelectionChanged += (_, _) => StartPrintCommand.NotifyCanExecuteChanged();
    }

    public PrintQueueViewModel Queue { get; }
    public PrintSettingsViewModel Settings { get; }

    [ObservableProperty] private bool _isPrinting;
    [ObservableProperty] private string _statusMessage = "准备就绪";
    [ObservableProperty] private AppStatusSeverity _statusSeverity = AppStatusSeverity.Informational;
    [ObservableProperty] private bool _isStatusOpen = true;

    public bool CanStartPrint =>
        !IsPrinting
        && Queue.Files.Any(f => f.IsSelected)
        && !string.IsNullOrWhiteSpace(Settings.SelectedPrinterName);

    partial void OnIsPrintingChanged(bool value)
    {
        Queue.IsPrinting = value;
        Queue.NotifyPrintingStateChanged();
        StartPrintCommand.NotifyCanExecuteChanged();
        CancelPrintCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanStartPrint))]
    private async Task StartPrintAsync()
    {
        var selected = Queue.Files.Where(f => f.IsSelected).ToList();
        var printerName = Settings.SelectedPrinterName;
        if (selected.Count == 0 || string.IsNullOrWhiteSpace(printerName)) return;

        foreach (var file in selected) file.ResetStatus();

        IsPrinting = true;
        _printCts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<(PrintFileItem file, int current, int total)>(r =>
                SetStatus($"正在打印 ({r.current}/{r.total})：{r.file.FileName}", AppStatusSeverity.Informational));

            var result = await _printQueue.ProcessAsync(
                selected, printerName!, Settings.UseColor, progress, _printCts.Token);

            if (result.Failed == 0)
                SetStatus($"打印完成，共 {result.Succeeded} 个文件。", AppStatusSeverity.Success);
            else
                SetStatus(
                    $"完成 {result.Succeeded} 个，失败 {result.Failed} 个：{string.Join("、", result.Failures.Select(f => f.File.FileName))}",
                    AppStatusSeverity.Warning);
        }
        catch (OperationCanceledException)
        {
            SetStatus("打印队列已取消。", AppStatusSeverity.Warning);
        }
        catch (Exception ex)
        {
            SetStatus($"打印失败：{ex.Message}", AppStatusSeverity.Error);
        }
        finally
        {
            IsPrinting = false;
            _printCts?.Dispose();
            _printCts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(IsPrinting))]
    private void CancelPrint()
    {
        _printCts?.Cancel();
        SetStatus("正在取消打印队列...", AppStatusSeverity.Warning);
    }

    private void OnQueueChanged(object? sender, QueueChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Message))
        {
            var severity = e.Count > 0 ? AppStatusSeverity.Success
                : e.Count < 0 ? AppStatusSeverity.Informational
                : AppStatusSeverity.Warning;
            SetStatus(e.Message, severity);
        }

        StartPrintCommand.NotifyCanExecuteChanged();
    }

    private void SetStatus(string message, AppStatusSeverity severity)
    {
        StatusMessage = message;
        StatusSeverity = severity;
        IsStatusOpen = !string.IsNullOrWhiteSpace(message);
    }
}
