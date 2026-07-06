using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Printing;

public interface IPrintFileItemFactory
{
    PrintFileItem Create(string filePath, string? folderRoot = null);

    bool IsSupported(string extension);
}

public sealed class PrintFileItemFactory : IPrintFileItemFactory
{
    private readonly IPrintHandlerRegistry _registry;

    public PrintFileItemFactory(IPrintHandlerRegistry registry) => _registry = registry;

    public PrintFileItem Create(string filePath, string? folderRoot = null)
    {
        var extension = Path.GetExtension(filePath);
        if (!_registry.IsSupported(extension))
            throw new NotSupportedException($"不支持的文件格式：{extension}");

        var kind = _registry.GetHandlerKind(extension);
        var handlerText = _registry.GetHandlerDisplayText(kind);
        return new PrintFileItem(filePath, kind, handlerText, folderRoot);
    }

    public bool IsSupported(string extension) => _registry.IsSupported(extension);
}
