using System.Drawing;
using Windows.Data.Pdf;
using Windows.Storage;
using WindowsPrinter.Infrastructure;
using WindowsPrinter.Models;
using WindowsPrinter.Services.Printing.Pdf;

namespace WindowsPrinter.Services.Printing.Preview;

public sealed class PrintPreviewService : IPrintPreviewService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".log", ".md", ".csv"
    };

    public async Task<PrintPreviewResult?> RenderPreviewAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(filePath))
            return null;

        var extension = Path.GetExtension(filePath);
        var fileName = Path.GetFileName(filePath);

        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return await RenderPdfPreviewAsync(filePath, fileName, cancellationToken);

        if (ImageExtensions.Contains(extension))
            return await Task.Run(() => RenderImagePreview(filePath, fileName), cancellationToken);

        if (TextExtensions.Contains(extension))
            return await Task.Run(() => RenderTextPreview(filePath, fileName), cancellationToken);

        return null;
    }

    private static async Task<PrintPreviewResult?> RenderPdfPreviewAsync(
        string filePath,
        string fileName,
        CancellationToken cancellationToken)
    {
        var storageFile = await StorageFile.GetFileFromPathAsync(filePath);
        var pdfDocument = await PdfDocument.LoadFromFileAsync(storageFile);
        if (pdfDocument.PageCount == 0)
            return null;

        cancellationToken.ThrowIfCancellationRequested();
        var pngBytes = await PdfPageRenderer.RenderPageToPngBytesAsync(pdfDocument, 0);
        return new PrintPreviewResult
        {
            PngBytes = pngBytes,
            Caption = $"{fileName} — 第 1 / {pdfDocument.PageCount} 页",
            PageCount = (int)pdfDocument.PageCount
        };
    }

    private static PrintPreviewResult RenderImagePreview(string filePath, string fileName)
    {
        using var image = Image.FromFile(filePath);
        return new PrintPreviewResult
        {
            PngBytes = GdiPrintHelper.RenderImageToPngBytes(image),
            Caption = fileName,
            PageCount = 1
        };
    }

    private static PrintPreviewResult RenderTextPreview(string filePath, string fileName) =>
        new()
        {
            PngBytes = GdiPrintHelper.RenderTextPreviewPng(filePath),
            Caption = $"{fileName} — 首页预览",
            PageCount = 1
        };
}
