namespace WindowsPrinter.Models;

public sealed class PrintSettings
{
    public required string PrinterName { get; init; }

    public bool UseColor { get; init; }
}
