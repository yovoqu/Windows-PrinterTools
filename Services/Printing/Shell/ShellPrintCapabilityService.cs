using WindowsPrinter.Infrastructure;
using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Printing.Shell;

public sealed class ShellPrintCapabilityService : IShellPrintCapabilityService
{
    internal static readonly HashSet<string> SupportedShellExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".rtf", ".webp"
    };

    public ShellPrintCapabilityResult Evaluate(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return ShellPrintCapabilityResult.Unavailable(ShellPrintFailureKind.FileNotFound, "文件路径无效。");

        if (!File.Exists(filePath))
            return ShellPrintCapabilityResult.Unavailable(
                ShellPrintFailureKind.FileNotFound,
                $"找不到文件：{Path.GetFileName(filePath)}");

        var extension = Path.GetExtension(filePath);
        if (!SupportedShellExtensions.Contains(extension))
        {
            return ShellPrintCapabilityResult.Unavailable(
                ShellPrintFailureKind.NoAssociation,
                $"格式“{extension}”不支持系统关联打印。");
        }

        if (!ShellAssociationHelper.HasPrintableAssociation(extension))
        {
            return ShellPrintCapabilityResult.Unavailable(
                ShellPrintFailureKind.NoAssociation,
                $"系统未找到可打印“{extension}”文件的关联程序。请安装对应应用（如 Microsoft Office、WPS）后重试。");
        }

        return ShellPrintCapabilityResult.Available();
    }
}
