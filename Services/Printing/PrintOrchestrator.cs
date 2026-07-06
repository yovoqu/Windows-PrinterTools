using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Printing;

public interface IPrintOrchestrator
{
    Task PrintSingleAsync(PrintFileItem file, PrintSettings settings, CancellationToken cancellationToken);
}

public sealed class PrintOrchestrator : IPrintOrchestrator
{
    private readonly IPrintHandlerRegistry _registry;

    public PrintOrchestrator(IPrintHandlerRegistry registry) => _registry = registry;

    public async Task PrintSingleAsync(PrintFileItem file, PrintSettings settings, CancellationToken cancellationToken)
    {
        var handler = _registry.Resolve(file.Extension);
        var request = new PrintRequest
        {
            FilePath = file.FilePath,
            PrinterName = settings.PrinterName,
            UseColor = settings.UseColor,
            Copies = settings.Copies,
            Duplex = settings.Duplex,
            ShellSpoolerTimeoutSeconds = settings.ShellSpoolerTimeoutSeconds
        };

        await handler.PrintAsync(request, cancellationToken);
    }
}
