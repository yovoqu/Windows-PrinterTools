using WindowsPrinter.Models;
using WindowsPrinter.Services.Printing.Handlers;

namespace WindowsPrinter.Services.Printing;

public interface IPrintHandlerRegistry
{
    IReadOnlySet<string> SupportedExtensions { get; }

    IFilePrintHandler Resolve(string extension);

    PrintHandlerKind GetHandlerKind(string extension);

    string GetHandlerDisplayText(PrintHandlerKind kind);

    bool IsSupported(string extension);
}

public sealed class PrintHandlerRegistry : IPrintHandlerRegistry
{
    private readonly IFilePrintHandler[] _handlers;

    public PrintHandlerRegistry(IEnumerable<IFilePrintHandler> handlers)
    {
        _handlers = handlers.ToArray();
        SupportedExtensions = BuildSupportedExtensions(_handlers);
    }

    public IReadOnlySet<string> SupportedExtensions { get; }

    public IFilePrintHandler Resolve(string extension)
    {
        var normalized = NormalizeExtension(extension);
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(normalized));
        return handler ?? throw new NotSupportedException($"不支持的文件格式：{normalized}");
    }

    public PrintHandlerKind GetHandlerKind(string extension) => Resolve(extension).Kind;

    public bool IsSupported(string extension) =>
        _handlers.Any(h => h.CanHandle(NormalizeExtension(extension)));

    public string GetHandlerDisplayText(PrintHandlerKind kind) => kind switch
    {
        PrintHandlerKind.Pdf => "原生 PDF",
        PrintHandlerKind.Image => "图片渲染",
        PrintHandlerKind.Text => "文本渲染",
        PrintHandlerKind.Shell => "系统关联程序",
        _ => "未知"
    };

    private static HashSet<string> BuildSupportedExtensions(IEnumerable<IFilePrintHandler> handlers)
    {
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var handler in handlers)
        {
            foreach (var extension in handler.SupportedExtensions)
                extensions.Add(NormalizeExtension(extension));
        }

        return extensions;
    }

    private static string NormalizeExtension(string extension) =>
        string.IsNullOrWhiteSpace(extension)
            ? string.Empty
            : extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
}
