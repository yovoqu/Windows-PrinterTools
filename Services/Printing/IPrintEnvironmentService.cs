using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Printing;

public interface IPrintEnvironmentService
{
    PrintEnvironmentResult ApplyPrintSettings(PrintSettings settings);
}
