using System.Drawing;
using Windows.Data.Pdf;
using Windows.Storage.Streams;

namespace WindowsPrinter.Services.Printing.Pdf;

internal static class PdfPageRenderer
{
    public static async Task<Image> RenderPageToImageAsync(PdfDocument pdfDocument, uint pageIndex)
    {
        using var pdfPage = pdfDocument.GetPage(pageIndex);
        using var stream = new InMemoryRandomAccessStream();
        var options = new PdfPageRenderOptions
        {
            DestinationWidth = (uint)Math.Max(1, pdfPage.Size.Width * 2),
            DestinationHeight = (uint)Math.Max(1, pdfPage.Size.Height * 2)
        };

        await pdfPage.RenderToStreamAsync(stream, options);
        stream.Seek(0);

        using var netStream = stream.AsStream();
        using var memory = new MemoryStream();
        await netStream.CopyToAsync(memory);
        memory.Position = 0;
        return Image.FromStream(memory);
    }

    public static async Task<byte[]> RenderPageToPngBytesAsync(PdfDocument pdfDocument, uint pageIndex = 0)
    {
        using var image = await RenderPageToImageAsync(pdfDocument, pageIndex);
        return Infrastructure.GdiPrintHelper.RenderImageToPngBytes(image);
    }
}
