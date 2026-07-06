namespace WindowsPrinter.Models;

public sealed class PrintPreviewResult
{
    public required byte[] PngBytes { get; init; }

    public required string Caption { get; init; }

    public int PageCount { get; init; } = 1;
}
