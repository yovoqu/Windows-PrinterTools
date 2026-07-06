using WindowsPrinter.Services.Abstractions;
using WinRT.Interop;

namespace WindowsPrinter.Services;

public sealed class WindowHandleProvider : IWindowHandleProvider
{
    public bool IsAvailable => App.MainWindow is not null;

    public IntPtr GetWindowHandle()
    {
        var window = App.MainWindow
            ?? throw new InvalidOperationException("Main window is not available.");
        return WindowNative.GetWindowHandle(window);
    }
}
