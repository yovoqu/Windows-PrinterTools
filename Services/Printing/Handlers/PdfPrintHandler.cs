using System.Drawing;
using System.Drawing.Printing;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using WindowsPrinter.Infrastructure;
using WindowsPrinter.Services.Printing.Handlers;

namespace WindowsPrinter.Services.Printing.Handlers;

public sealed class PdfPrintHandler : IFilePrintHandler
{
    public PrintHandlerKind Kind => PrintHandlerKind.Pdf;

    public bool CanHandle(string extension) => extension == ".pdf";

    public async Task PrintAsync(PrintRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storageFile = await StorageFile.GetFileFromPathAsync(request.FilePath);
        var pdfDocument = await PdfDocument.LoadFromFileAsync(storageFile);
        if (pdfDocument.PageCount == 0)
            throw new InvalidOperationException("PDF 文件没有可打印页面。");

        var pageImages = new List<Image>();
        try
        {
            for (uint pageIndex = 0; pageIndex < pdfDocument.PageCount; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var pdfPage = pdfDocument.GetPage(pageIndex);
                using var stream = new InMemoryRandomAccessStream();
                var options = new PdfPageRenderOptions
                {
                    DestinationWidth = (uint)Math.Max(1, pdfPage.Size.Width * 2),
                    DestinationHeight = (uint)Math.Max(1, pdfPage.Size.Height * 2)
                };
                await pdfPage.RenderToStreamAsync(stream, options);
                stream.Seek(0);
                pageImages.Add(await StreamToImageAsync(stream));
            }

            await Task.Run(() => PrintImages(pageImages, request), cancellationToken);
        }
        finally
        {
            foreach (var image in pageImages) image.Dispose();
        }
    }

    private static async Task<Image> StreamToImageAsync(IRandomAccessStream stream)
    {
        using var netStream = stream.AsStream();
        using var memory = new MemoryStream();
        await netStream.CopyToAsync(memory);
        memory.Position = 0;
        return Image.FromStream(memory);
    }

    private static void PrintImages(IReadOnlyList<Image> pages, PrintRequest request)
    {
        var pageIndex = 0;
        using var printDocument = GdiPrintHelper.CreatePrintDocument(request.PrinterName);
        printDocument.PrintPage += (_, e) =>
        {
            GdiPrintHelper.DrawScaledImage(e.Graphics!, pages[pageIndex], e.MarginBounds, request.UseColor);
            pageIndex++;
            e.HasMorePages = pageIndex < pages.Count;
        };
        printDocument.Print();
    }
}
