using System.Runtime.InteropServices;
using System.Text;

namespace WindowsPrinter.Infrastructure;

internal static class ShellAssociationHelper
{
    private enum AssocStr
    {
        Command = 1,
        Executable = 2,
        FriendlyDocName = 3
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, BestFitMapping = false, SetLastError = true)]
    private static extern int AssocQueryString(
        int flags,
        AssocStr str,
        string pszAssoc,
        string? pszExtra,
        StringBuilder pszOut,
        ref uint pcchOut);

    public static bool HasPrintableAssociation(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        var normalized = extension.StartsWith('.') ? extension : $".{extension}";
        return TryQueryExecutable(normalized) || TryQueryExecutable(normalized.TrimStart('.'));
    }

    private static bool TryQueryExecutable(string assoc)
    {
        var buffer = new StringBuilder(1024);
        uint size = (uint)buffer.Capacity;
        var hr = AssocQueryString(0, AssocStr.Executable, assoc, null, buffer, ref size);
        return hr == 0 && buffer.Length > 0;
    }
}
