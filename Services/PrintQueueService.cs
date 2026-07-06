using WindowsPrinter.Models;
using WindowsPrinter.Services.Abstractions;
using WindowsPrinter.Services.Logging;
using WindowsPrinter.Services.Printing;

namespace WindowsPrinter.Services;

public sealed class PrintQueueService : IPrintQueueService
{
    private readonly IPrintOrchestrator _orchestrator;
    private readonly IPrintEnvironmentService _environment;
    private readonly IPrintSessionLog _log;

    public PrintQueueService(
        IPrintOrchestrator orchestrator,
        IPrintEnvironmentService environment,
        IPrintSessionLog log)
    {
        _orchestrator = orchestrator;
        _environment = environment;
        _log = log;
    }

    public async Task<PrintQueueResult> ProcessAsync(
        IReadOnlyList<PrintFileItem> files,
        PrintSettings settings,
        IProgress<(PrintFileItem file, int current, int total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (files.Count == 0) throw new InvalidOperationException("请至少选择一个文件。");
        if (string.IsNullOrWhiteSpace(settings.PrinterName)) throw new InvalidOperationException("请选择打印机。");

        var environmentResult = _environment.ApplyPrintSettings(settings);
        if (!environmentResult.Succeeded)
        {
            _log.Error("PrintQueue", environmentResult.Message ?? "无法应用打印设置。");
            throw new InvalidOperationException(environmentResult.Message ?? "无法应用打印设置。");
        }

        _log.Information("PrintQueue", $"已应用打印设置：{settings.PrinterName}，{settings.Copies} 份，{(settings.UseColor ? "彩色" : "黑白")}。");

        var failures = new List<(PrintFileItem File, string Error)>();
        var succeeded = 0;

        for (var index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = files[index];
            file.Status = PrintJobStatus.Printing;
            progress?.Report((file, index + 1, files.Count));
            _log.Information("PrintQueue", "开始打印", file.FileName);

            try
            {
                await _orchestrator.PrintSingleAsync(file, settings, cancellationToken);
                file.Status = PrintJobStatus.Completed;
                file.ErrorMessage = null;
                succeeded++;
                _log.Success("PrintQueue", "打印完成", file.FileName);
            }
            catch (OperationCanceledException)
            {
                file.Status = PrintJobStatus.Cancelled;
                file.ErrorMessage = "已取消";
                _log.Warning("PrintQueue", "已取消", file.FileName);
                MarkRemainingCancelled(files, index + 1);
                throw;
            }
            catch (Exception ex)
            {
                file.Status = PrintJobStatus.Failed;
                file.ErrorMessage = ex.Message;
                failures.Add((file, ex.Message));
                _log.Error("PrintQueue", ex.Message, file.FileName);
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

    private void MarkRemainingCancelled(IReadOnlyList<PrintFileItem> files, int startIndex)
    {
        for (var i = startIndex; i < files.Count; i++)
        {
            files[i].Status = PrintJobStatus.Cancelled;
            files[i].ErrorMessage = "已取消";
            _log.Warning("PrintQueue", "已取消（批次中止）", files[i].FileName);
        }
    }
}
