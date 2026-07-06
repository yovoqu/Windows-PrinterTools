using WindowsPrinter.Infrastructure;
using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Discovery;

internal static class WinSpoolPrinterProvider
{
    public static IReadOnlyList<PrinterInfo> GetPrinters()
    {
        return NativePrintHelper.GetInstalledPrinters()
            .Select(name => new PrinterInfo
            {
                Name = name,
                DiscoverySource = "Spooler"
            })
            .ToList();
    }
}
