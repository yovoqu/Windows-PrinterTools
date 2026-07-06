using WindowsPrinter.Models;

namespace WindowsPrinter.Services.Printing.Shell;

public sealed class ShellPrintException : Exception
{
    public ShellPrintException(ShellPrintFailureKind kind, string message) : base(message) => Kind = kind;

    public ShellPrintFailureKind Kind { get; }
}

public interface IShellPrintService
{
    Task PrintAsync(PrintRequest request, CancellationToken cancellationToken);
}
