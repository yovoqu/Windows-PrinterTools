using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Discovery;

public interface IPrinterDiscoveryService
{
    Task<IReadOnlyList<PrinterInfo>> DiscoverAsync(CancellationToken cancellationToken = default);

    string? GetDefaultPrinterName();
}
