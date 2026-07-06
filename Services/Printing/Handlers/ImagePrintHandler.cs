using System.Drawing;
using System.Drawing.Printing;
using WindowsPrinter.Infrastructure;
using WindowsPrinter.Services.Printing.Handlers;

namespace WindowsPrinter.Services.Printing.Handlers;

public sealed class ImagePrintHandler : IFilePrintHandler
{
    private static readonly HashSet<string> Extensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff"];

    public PrintHandlerKind Kind => PrintHandlerKind.Image;

    public bool CanHandle(string extension) => Extensions.Contains(extension);

    public Task PrintAsync(PrintRequest request, CancellationToken cancellationToken)
    {
        if (request.FilePath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            return ShellPrintHandler.PrintViaShellAsync(request, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            using var image = Image.FromFile(request.FilePath);
            using var printDocument = GdiPrintHelper.CreatePrintDocument(request.PrinterName);
            printDocument.PrintPage += (_, e) =>
            {
                GdiPrintHelper.DrawScaledImage(e.Graphics!, image, e.MarginBounds, request.UseColor);
                e.HasMorePages = false;
            };
            printDocument.Print();
        }, cancellationToken);
    }
}
