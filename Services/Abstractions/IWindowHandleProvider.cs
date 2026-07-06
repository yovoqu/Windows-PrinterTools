namespace WindowsPrinter.Services.Abstractions;

public interface IWindowHandleProvider
{
    bool IsAvailable { get; }

    IntPtr GetWindowHandle();
}
