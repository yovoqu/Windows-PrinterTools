using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Printing.Shell;

public sealed class ShellPrintCapabilityResult
{
    public bool CanPrint { get; init; }

    public ShellPrintFailureKind? FailureKind { get; init; }

    public string? Message { get; init; }

    public static ShellPrintCapabilityResult Available() => new() { CanPrint = true };

    public static ShellPrintCapabilityResult Unavailable(ShellPrintFailureKind kind, string message) =>
        new() { CanPrint = false, FailureKind = kind, Message = message };
}

public interface IShellPrintCapabilityService
{
    ShellPrintCapabilityResult Evaluate(string filePath);
}
