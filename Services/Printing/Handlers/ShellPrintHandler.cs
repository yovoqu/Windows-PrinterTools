using WindowsPrinter.Infrastructure;
using WindowsPrinter.Services.Printing.Handlers;

namespace WindowsPrinter.Services.Printing.Handlers;

public static class ShellPrintHandler
{
    private static readonly HashSet<string> Extensions =
        [".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".rtf", ".webp"];

    public static bool CanHandle(string extension) => Extensions.Contains(extension);

    public static Task PrintViaShellAsync(PrintRequest request, CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            NativePrintHelper.PrintWithShell(request.FilePath, request.PrinterName);
            NativePrintHelper.WaitForSpoolerJob(request.PrinterName, TimeSpan.FromSeconds(45), cancellationToken);
        }, cancellationToken);
}

public sealed class ShellPrintHandlerAdapter : IFilePrintHandler
{
    public PrintHandlerKind Kind => PrintHandlerKind.Shell;

    public bool CanHandle(string extension) => ShellPrintHandler.CanHandle(extension);

    public Task PrintAsync(PrintRequest request, CancellationToken cancellationToken) =>
        ShellPrintHandler.PrintViaShellAsync(request, cancellationToken);
}
