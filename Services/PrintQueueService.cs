using WindowsPrinter.Models;
using WindowsPrinter.Services.Abstractions;
using WindowsPrinter.Services.Printing;

namespace WindowsPrinter.Services;

public sealed class PrintQueueService : IPrintQueueService
{
    private readonly PrintOrchestrator _orchestrator = new();

    public async Task<PrintQueueResult> ProcessAsync(
        IReadOnlyList<PrintFileItem> files,
        string printerName,
        bool useColor,
        IProgress<(PrintFileItem file, int current, int total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (files.Count == 0) throw new InvalidOperationException("请至少选择一个文件。");
        if (string.IsNullOrWhiteSpace(printerName)) throw new InvalidOperationException("请选择打印机。");

        var settings = new PrintSettings { PrinterName = printerName, UseColor = useColor };
        var failures = new List<(PrintFileItem File, string Error)>();
        var succeeded = 0;

        for (var index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = files[index];
            file.Status = PrintJobStatus.Printing;
            progress?.Report((file, index + 1, files.Count));

            try
            {
                await _orchestrator.PrintSingleAsync(file, settings, cancellationToken);
                file.Status = PrintJobStatus.Completed;
                file.ErrorMessage = null;
                succeeded++;
            }
            catch (OperationCanceledException)
            {
                file.Status = PrintJobStatus.Cancelled;
                file.ErrorMessage = "已取消";
                throw;
            }
            catch (Exception ex)
            {
                file.Status = PrintJobStatus.Failed;
                file.ErrorMessage = ex.Message;
                failures.Add((file, ex.Message));
            }
        }

        return new PrintQueueResult
        {
            Total = files.Count,
            Succeeded = succeeded,
            Failed = failures.Count,
            Failures = failures
        };
    }
}
