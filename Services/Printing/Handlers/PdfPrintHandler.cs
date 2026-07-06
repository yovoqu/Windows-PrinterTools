using Windows.Data.Pdf;
using Windows.Storage;
using WindowsPrinter.Models;
using WindowsPrinter.Services.Printing.Pdf;

namespace WindowsPrinter.Services.Printing.Handlers;

public sealed class PdfPrintHandler : IFilePrintHandler
{
    public PrintHandlerKind Kind => PrintHandlerKind.Pdf;

    public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf" };

    public bool CanHandle(string extension) => SupportedExtensions.Contains(extension);

    public async Task PrintAsync(PrintRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storageFile = await StorageFile.GetFileFromPathAsync(request.FilePath);
        var pdfDocument = await PdfDocument.LoadFromFileAsync(storageFile);
        if (pdfDocument.PageCount == 0)
            throw new InvalidOperationException("PDF 文件没有可打印页面。");

        await PdfStreamingPrintSession.PrintAsync(pdfDocument, request, cancellationToken);
    }
}
