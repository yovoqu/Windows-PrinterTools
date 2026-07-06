using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Abstractions;

public sealed class PrintQueueResult
{
    public int Total { get; init; }

    public int Succeeded { get; init; }

    public int Failed { get; init; }

    public IReadOnlyList<(PrintFileItem File, string Error)> Failures { get; init; } = [];
}

public interface IPrintQueueService
{
    Task<PrintQueueResult> ProcessAsync(
        IReadOnlyList<PrintFileItem> files,
        string printerName,
        bool useColor,
        IProgress<(PrintFileItem file, int current, int total)>? progress = null,
        CancellationToken cancellationToken = default);
}
