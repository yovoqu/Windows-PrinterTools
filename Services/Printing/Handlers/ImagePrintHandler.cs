using System.Drawing;
using System.Drawing.Printing;
using WindowsPrinter.Infrastructure;
using WindowsPrinter.Models;
using WindowsPrinter.Services.Printing.Shell;

namespace WindowsPrinter.Services.Printing.Handlers;

public sealed class ImagePrintHandler : IFilePrintHandler
{
    public PrintHandlerKind Kind => PrintHandlerKind.Image;

    public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff"
    };

    public bool CanHandle(string extension) => SupportedExtensions.Contains(extension);

    public Task PrintAsync(PrintRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            using var image = Image.FromFile(request.FilePath);
            using var printDocument = GdiPrintHelper.CreatePrintDocument(request.PrinterName);
            printDocument.PrintPage += (_, e) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                GdiPrintHelper.DrawScaledImage(e.Graphics!, image, e.MarginBounds, request.UseColor);
                e.HasMorePages = false;
            };
            printDocument.Print();
        }, cancellationToken);
    }
}
