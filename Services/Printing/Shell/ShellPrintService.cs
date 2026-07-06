using WindowsPrinter.Infrastructure;
using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Printing.Shell;

public sealed class ShellPrintService : IShellPrintService
{
    private readonly IShellPrintCapabilityService _capability;

    public ShellPrintService(IShellPrintCapabilityService capability) => _capability = capability;

    public Task PrintAsync(PrintRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var capability = _capability.Evaluate(request.FilePath);
        if (!capability.CanPrint)
        {
            throw new ShellPrintException(
                capability.FailureKind ?? ShellPrintFailureKind.NoAssociation,
                capability.Message ?? "无法通过系统关联程序打印此文件。");
        }

        return Task.Run(() =>
        {
            try
            {
                NativePrintHelper.PrintWithShell(request.FilePath, request.PrinterName);
                var timeout = TimeSpan.FromSeconds(Math.Clamp(request.ShellSpoolerTimeoutSeconds, 15, 300));
                NativePrintHelper.WaitForSpoolerJob(request.PrinterName, timeout, cancellationToken);
            }
            catch (TimeoutException ex)
            {
                throw new ShellPrintException(ShellPrintFailureKind.SpoolerTimeout, ex.Message);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ShellPrintException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ShellPrintException(
                    ShellPrintFailureKind.ShellExecuteFailed,
                    $"系统无法打印文件：{Path.GetFileName(request.FilePath)}（{ex.Message}）");
            }
        }, cancellationToken);
    }
}
