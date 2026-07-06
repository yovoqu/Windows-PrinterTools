using System.Drawing.Printing;
using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Discovery;

public sealed class CompositePrinterDiscoveryService : IPrinterDiscoveryService
{
    public Task<IReadOnlyList<PrinterInfo>> DiscoverAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var merged = new Dictionary<string, PrinterInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var printer in DrawingPrinterProvider.GetPrinters())
                Merge(merged, printer);

            foreach (var printer in WinSpoolPrinterProvider.GetPrinters())
                Merge(merged, printer);

            var defaultName = GetDefaultPrinterName();
            var result = merged.Values
                .Select(p => new PrinterInfo
                {
                    Name = p.Name,
                    DiscoverySource = p.DiscoverySource,
                    IsDefault = !string.IsNullOrWhiteSpace(defaultName)
                        && string.Equals(p.Name, defaultName, StringComparison.OrdinalIgnoreCase)
                })
                .OrderBy(p => p.IsDefault ? 0 : 1)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return (IReadOnlyList<PrinterInfo>)result;
        }, cancellationToken);

    public string? GetDefaultPrinterName()
    {
        try
        {
            var name = new PrinterSettings().PrinterName;
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        catch
        {
            return null;
        }
    }

    private static void Merge(Dictionary<string, PrinterInfo> merged, PrinterInfo printer)
    {
        if (merged.TryGetValue(printer.Name, out var existing))
        {
            merged[printer.Name] = new PrinterInfo
            {
                Name = printer.Name,
                DiscoverySource = $"{existing.DiscoverySource}+{printer.DiscoverySource}",
                IsDefault = existing.IsDefault || printer.IsDefault
            };
        }
        else
        {
            merged[printer.Name] = printer;
        }
    }
}
