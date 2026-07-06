using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Printing.Preview;

public interface IPrintPreviewService
{
    Task<PrintPreviewResult?> RenderPreviewAsync(string filePath, CancellationToken cancellationToken = default);
}
