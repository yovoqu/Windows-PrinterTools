using WindowsPrinter.Services.Printing.Handlers;

namespace WindowsPrinter.Services.Printing;

public static class PrintHandlerFactory
{
    public static readonly HashSet<string> SupportedExtensions =
    [
        ".pdf", ".txt", ".log", ".md", ".csv",
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".webp",
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".rtf"
    ];

    private static readonly IFilePrintHandler[] Handlers =
    [
        new PdfPrintHandler(),
        new ImagePrintHandler(),
        new TextPrintHandler(),
        new ShellPrintHandlerAdapter()
    ];

    public static IFilePrintHandler Resolve(string extension)
    {
        var handler = Handlers.FirstOrDefault(h => h.CanHandle(extension));
        return handler ?? throw new NotSupportedException($"不支持的文件格式：{extension}");
    }

    public static PrintHandlerKind GetHandlerKind(string extension) => Resolve(extension).Kind;
}
