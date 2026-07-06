namespace WindowsPrinter.Models;

public sealed class PrintSettings
{
    public required string PrinterName { get; init; }

    public bool UseColor { get; init; }

    public short Copies { get; init; } = 1;

    public PrintDuplexMode Duplex { get; init; } = PrintDuplexMode.Simplex;

    public int ShellSpoolerTimeoutSeconds { get; init; } = 60;
}
