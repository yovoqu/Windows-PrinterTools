using WindowsPrinter.Infrastructure;
using WindowsPrinter.Models;
using WindowsPrinter.Services.Printing.Handlers;

namespace WindowsPrinter.Services.Printing;

public sealed class PrintOrchestrator
{
    public async Task PrintSingleAsync(PrintFileItem file, PrintSettings settings, CancellationToken cancellationToken)
    {
        NativePrintHelper.ApplyColorMode(settings.PrinterName, settings.UseColor);

        var handler = PrintHandlerFactory.Resolve(file.Extension);
        var request = new PrintRequest
        {
            FilePath = file.FilePath,
            PrinterName = settings.PrinterName,
            UseColor = settings.UseColor
        };

        await handler.PrintAsync(request, cancellationToken);
    }
}
