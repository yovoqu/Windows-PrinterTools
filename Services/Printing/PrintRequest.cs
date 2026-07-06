namespace WindowsPrinter.Services.Printing;

public sealed class PrintRequest
{
    public required string FilePath { get; init; }

    public required string PrinterName { get; init; }

    public required bool UseColor { get; init; }
}
