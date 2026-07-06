using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Printing;

public sealed class PrintRequest
{
    public required string FilePath { get; init; }

    public required string PrinterName { get; init; }

    public required bool UseColor { get; init; }

    public short Copies { get; init; } = 1;

    public PrintDuplexMode Duplex { get; init; } = PrintDuplexMode.Simplex;

    public int ShellSpoolerTimeoutSeconds { get; init; } = 60;
}
