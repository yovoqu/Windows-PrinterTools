using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using WindowsPrinter.Infrastructure;

namespace WindowsPrinter.Infrastructure;

internal static class GdiPrintHelper
{
    public static PrintDocument CreatePrintDocument(string printerName) =>
        new()
        {
            PrinterSettings = new PrinterSettings { PrinterName = printerName }
        };

    public static void DrawScaledImage(Graphics graphics, Image image, Rectangle bounds, bool useColor)
    {
        var ratio = Math.Min((float)bounds.Width / image.Width, (float)bounds.Height / image.Height);
        var width = (int)(image.Width * ratio);
        var height = (int)(image.Height * ratio);
        var x = bounds.Left + (bounds.Width - width) / 2;
        var y = bounds.Top + (bounds.Height - height) / 2;

        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        if (useColor)
        {
            graphics.DrawImage(image, x, y, width, height);
            return;
        }

        using var attributes = CreateGrayscaleAttributes();
        graphics.DrawImage(
            image,
            new Rectangle(x, y, width, height),
            0, 0, image.Width, image.Height,
            GraphicsUnit.Pixel,
            attributes);
    }

    public static ImageAttributes CreateGrayscaleAttributes()
    {
        var matrix = new ColorMatrix(new float[][]
        {
            [0.299f, 0.299f, 0.299f, 0, 0],
            [0.587f, 0.587f, 0.587f, 0, 0],
            [0.114f, 0.114f, 0.114f, 0, 0],
            [0, 0, 0, 1, 0],
            [0, 0, 0, 0, 1]
        });

        var attributes = new ImageAttributes();
        attributes.SetColorMatrix(matrix);
        return attributes;
    }

    public static string ReadTextWithEncoding(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    public static byte[] RenderImageToPngBytes(Image image)
    {
        using var memory = new MemoryStream();
        image.Save(memory, ImageFormat.Png);
        return memory.ToArray();
    }

    public static byte[] RenderTextPreviewPng(string filePath, int maxLines = 40)
    {
        var content = ReadTextWithEncoding(filePath);
        var lines = content.Replace("\r\n", "\n").Split('\n').Take(maxLines).ToArray();

        const int width = 816;
        const int height = 1056;
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        using var font = new Font("Consolas", 10);
        using var brush = new SolidBrush(Color.Black);

        var y = 24;
        var lineHeight = (int)graphics.MeasureString("A", font).Height;
        foreach (var line in lines)
        {
            if (y + lineHeight > height - 24) break;
            graphics.DrawString(line, font, brush, 24, y);
            y += lineHeight;
        }

        return RenderImageToPngBytes(bitmap);
    }
}
