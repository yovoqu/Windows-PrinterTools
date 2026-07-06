using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Printing.Handlers;

public interface IFilePrintHandler
{
    PrintHandlerKind Kind { get; }

    IReadOnlySet<string> SupportedExtensions { get; }

    bool CanHandle(string extension);

    Task PrintAsync(PrintRequest request, CancellationToken cancellationToken);
}
