using System.Drawing;
using System.Drawing.Printing;
using WindowsPrinter.Infrastructure;
using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Printing.Handlers;

public sealed class TextPrintHandler : IFilePrintHandler
{
    public PrintHandlerKind Kind => PrintHandlerKind.Text;

    public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".log", ".md", ".csv"
    };

    public bool CanHandle(string extension) => SupportedExtensions.Contains(extension);

    public Task PrintAsync(PrintRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            var content = GdiPrintHelper.ReadTextWithEncoding(request.FilePath);
            var lines = content.Replace("\r\n", "\n").Split('\n');
            var lineIndex = 0;

            using var printDocument = GdiPrintHelper.CreatePrintDocument(request.PrinterName);
            printDocument.PrintPage += (_, e) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var font = new Font("Consolas", 10);
                using var brush = new SolidBrush(Color.Black);
                var bounds = e.MarginBounds;
                var lineHeight = (int)e.Graphics!.MeasureString("A", font).Height;
                var y = bounds.Top;

                while (lineIndex < lines.Length && y + lineHeight <= bounds.Bottom)
                {
                    e.Graphics.DrawString(lines[lineIndex], font, brush, bounds.Left, y);
                    y += lineHeight;
                    lineIndex++;
                }

                e.HasMorePages = lineIndex < lines.Length;
            };
            printDocument.Print();
        }, cancellationToken);
    }
}
