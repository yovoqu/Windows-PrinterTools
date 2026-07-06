using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Printing;

public sealed class PrintEnvironmentService : IPrintEnvironmentService
{
    public PrintEnvironmentResult ApplyPrintSettings(PrintSettings settings) =>
        Infrastructure.NativePrintHelper.ApplyPrintSettings(settings);
}
