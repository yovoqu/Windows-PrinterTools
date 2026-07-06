namespace WindowsPrinter.Models;

public enum ShellPrintFailureKind
{
    FileNotFound,
    NoAssociation,
    ShellExecuteFailed,
    SpoolerTimeout,
    Cancelled
}
