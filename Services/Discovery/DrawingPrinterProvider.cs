using System.Drawing.Printing;
using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Discovery;

internal static class DrawingPrinterProvider
{
    public static IReadOnlyList<PrinterInfo> GetPrinters()
    {
        var printers = new List<PrinterInfo>();
        try
        {
            foreach (string name in PrinterSettings.InstalledPrinters)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    printers.Add(new PrinterInfo
                    {
                        Name = name,
                        DiscoverySource = "Drawing"
                    });
                }
            }
        }
        catch
        {
            // ignored — composite service merges other sources
        }

        return printers;
    }
}
