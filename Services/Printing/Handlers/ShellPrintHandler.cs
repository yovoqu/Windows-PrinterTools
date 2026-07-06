using WindowsPrinter.Models;
using WindowsPrinter.Services.Printing.Shell;

namespace WindowsPrinter.Services.Printing.Handlers;

public sealed class ShellPrintHandlerAdapter : IFilePrintHandler
{
    private readonly IShellPrintService _shellPrint;

    public ShellPrintHandlerAdapter(IShellPrintService shellPrint) => _shellPrint = shellPrint;

    public PrintHandlerKind Kind => PrintHandlerKind.Shell;

    public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".rtf", ".webp"
    };

    public bool CanHandle(string extension) => SupportedExtensions.Contains(extension);

    public Task PrintAsync(PrintRequest request, CancellationToken cancellationToken) =>
        _shellPrint.PrintAsync(request, cancellationToken);
}
