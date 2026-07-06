namespace WindowsPrinter.Services.Printing.Handlers;

public interface IFilePrintHandler
{
    PrintHandlerKind Kind { get; }

    bool CanHandle(string extension);

    Task PrintAsync(PrintRequest request, CancellationToken cancellationToken);
}
