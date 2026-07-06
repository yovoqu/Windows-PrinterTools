using System.Runtime.InteropServices;

namespace WindowsPrinter.Infrastructure;

internal static class NativePrintHelper
{
    private const int DmColor = 0x00000800;
    private const short DmColorMonochrome = 1;
    private const short DmColorColor = 2;
    private const uint PrinterEnumLocal = 0x00000002;
    private const uint PrinterEnumConnections = 0x00000010;

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool EnumPrinters(uint flags, string? name, uint level, IntPtr buffer, uint bufferSize, out uint needed, out uint returned);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinter(string printerName, out IntPtr handle, IntPtr defaults);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr handle);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int DocumentProperties(IntPtr hwnd, IntPtr printer, string deviceName, IntPtr devModeOutput, IntPtr devModeInput, uint mode);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetPrinter(IntPtr printer, uint level, IntPtr data, uint command);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr ShellExecute(IntPtr hwnd, string operation, string file, string parameters, string directory, int showCmd);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool EnumJobs(string printerName, uint firstJob, uint numberOfJobs, uint level, IntPtr jobs, uint bufferSize, out uint bytesNeeded, out uint jobsReturned);

    [StructLayout(LayoutKind.Sequential)]
    private struct PrinterInfo4
    {
        public IntPtr pPrinterName;
        public IntPtr pServerName;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PrinterInfo2
    {
        public IntPtr pServerName;
        public IntPtr pPrinterName;
        public IntPtr pShareName;
        public IntPtr pPortName;
        public IntPtr pDriverName;
        public IntPtr pComment;
        public IntPtr pLocation;
        public IntPtr pDevMode;
        public IntPtr pSepFile;
        public IntPtr pPrintProcessor;
        public IntPtr pDatatype;
        public IntPtr pParameters;
        public IntPtr pSecurityDescriptor;
        public uint Attributes;
        public uint Priority;
        public uint DefaultPriority;
        public uint StartTime;
        public uint UntilTime;
        public uint Status;
        public uint cJobs;
        public uint AveragePPM;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DmDeviceName;
        public short DmSpecVersion;
        public short DmDriverVersion;
        public short DmSize;
        public short DmDriverExtra;
        public int DmFields;
        public short DmOrientation;
        public short DmPaperSize;
        public short DmPaperLength;
        public short DmPaperWidth;
        public short DmScale;
        public short DmCopies;
        public short DmDefaultSource;
        public short DmPrintQuality;
        public short DmColor;
        public short DmDuplex;
        public short DmYResolution;
        public short DmTTOption;
        public short DmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DmFormName;
        public short DmLogPixels;
        public int DmBitsPerPel;
        public int DmPelsWidth;
        public int DmPelsHeight;
        public int DmDisplayFlags;
        public int DmDisplayFrequency;
        public int DmICMMethod;
        public int DmICMIntent;
        public int DmMediaType;
        public int DmDitherType;
        public int DmReserved1;
        public int DmReserved2;
        public int DmPanningWidth;
        public int DmPanningHeight;
    }

    public static IReadOnlyList<string> GetInstalledPrinters()
    {
        const uint level = 4;
        var printers = new List<string>();
        EnumPrinters(PrinterEnumLocal | PrinterEnumConnections, null, level, IntPtr.Zero, 0, out var needed, out _);
        if (needed == 0) return printers;

        var buffer = Marshal.AllocHGlobal((int)needed);
        try
        {
            if (!EnumPrinters(PrinterEnumLocal | PrinterEnumConnections, null, level, buffer, needed, out _, out var returned))
                return printers;

            var entrySize = Marshal.SizeOf<PrinterInfo4>();
            for (uint i = 0; i < returned; i++)
            {
                var info = Marshal.PtrToStructure<PrinterInfo4>(buffer + (int)(i * entrySize));
                var printerName = Marshal.PtrToStringUni(info.pPrinterName);
                if (!string.IsNullOrWhiteSpace(printerName))
                    printers.Add(printerName);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return printers.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p).ToList();
    }

    public static void ApplyColorMode(string printerName, bool useColor)
    {
        if (!OpenPrinter(printerName, out var printerHandle, IntPtr.Zero))
            throw new InvalidOperationException($"无法打开打印机：{printerName}");

        try
        {
            var requiredSize = DocumentProperties(IntPtr.Zero, printerHandle, printerName, IntPtr.Zero, IntPtr.Zero, 0);
            if (requiredSize <= 0) return;

            var devModePtr = Marshal.AllocHGlobal(requiredSize);
            try
            {
                if (DocumentProperties(IntPtr.Zero, printerHandle, printerName, devModePtr, IntPtr.Zero, 0x2) < 0) return;

                var devMode = Marshal.PtrToStructure<DevMode>(devModePtr);
                devMode.DmFields |= DmColor;
                devMode.DmColor = useColor ? DmColorColor : DmColorMonochrome;
                Marshal.StructureToPtr(devMode, devModePtr, false);

                if (DocumentProperties(IntPtr.Zero, printerHandle, printerName, devModePtr, devModePtr, 0xA) < 0) return;
                SetPrinter(printerHandle, 9, devModePtr, 0);
            }
            finally
            {
                Marshal.FreeHGlobal(devModePtr);
            }
        }
        finally
        {
            ClosePrinter(printerHandle);
        }
    }

    public static void PrintWithShell(string filePath, string printerName)
    {
        var result = ShellExecute(IntPtr.Zero, "printto", filePath, $"\"{printerName}\"", Path.GetDirectoryName(filePath) ?? string.Empty, 0);
        if ((nint)result <= 32)
            throw new InvalidOperationException($"系统无法打印文件：{Path.GetFileName(filePath)}（错误代码 {(int)result}）");
    }

    public static void WaitForSpoolerJob(string printerName, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var baseline = GetJobCount(printerName);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (GetJobCount(printerName) > baseline)
            {
                Thread.Sleep(500);
                return;
            }
            Thread.Sleep(200);
        }
    }

    private static int GetJobCount(string printerName)
    {
        if (!OpenPrinter(printerName, out var printerHandle, IntPtr.Zero)) return 0;
        try
        {
            EnumJobs(printerName, 0, 255, 1, IntPtr.Zero, 0, out var needed, out _);
            if (needed == 0) return 0;

            var buffer = Marshal.AllocHGlobal((int)needed);
            try
            {
                return EnumJobs(printerName, 0, 255, 1, buffer, needed, out _, out var returned) ? (int)returned : 0;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            ClosePrinter(printerHandle);
        }
    }
}
