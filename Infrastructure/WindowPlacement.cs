using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace WindowsPrinter.Infrastructure;

/// <summary>
/// Window sizing aligned with Microsoft responsive breakpoints:
/// Large >= 1008 epx, Medium 641–1007, Small &lt;= 640.
/// </summary>
public static class WindowPlacement
{
    /// <summary>Minimum width for queue + right sidebar layout.</summary>
    public const int MinWidth = 960;

    public const int MinHeight = 640;

    public const int PreferredDefaultWidth = 1280;

    public const int PreferredDefaultHeight = 800;

    public const int MaxDefaultWidth = 1440;

    public const int MaxDefaultHeight = 900;

    /// <summary>Standard breakpoint for full sidebar width (380px).</summary>
    public const int WideLayoutBreakpoint = 1008;

    public static void Apply(Window window)
    {
        var appWindow = window.AppWindow;
        if (appWindow is null)
            return;

        appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        if (appWindow.Presenter is OverlappedPresenter overlapped)
        {
            overlapped.IsResizable = true;
            overlapped.IsMinimizable = true;
            overlapped.IsMaximizable = true;
            overlapped.PreferredMinimumWidth = MinWidth;
            overlapped.PreferredMinimumHeight = MinHeight;
        }

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            window.ExtendsContentIntoTitleBar = true;
            if (window.Content is FrameworkElement root
                && root.FindName("AppTitleBar") is UIElement titleBar)
            {
                window.SetTitleBar(titleBar);
            }
        }

        TrySetIcon(appWindow);
    }

    /// <summary>
    /// Apply default size after the window is activated — resizing in the ctor often does not stick.
    /// Safe to call multiple times.
    /// </summary>
    public static void EnsureDefaultSize(Window window)
    {
        var appWindow = window.AppWindow;
        if (appWindow is null)
            return;

        var display = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        var (width, height) = GetDefaultSize(display);
        appWindow.Resize(new SizeInt32(width, height));

        if (display is not null)
            CenterOnWorkArea(appWindow, display, width, height);
    }

    public static (int Width, int Height) GetDefaultSize(DisplayArea? display)
    {
        if (display is null)
            return (PreferredDefaultWidth, PreferredDefaultHeight);

        var work = display.WorkArea;
        var width = Math.Clamp((int)(work.Width * 0.90), MinWidth, Math.Min(MaxDefaultWidth, work.Width));
        var height = Math.Clamp((int)(work.Height * 0.85), MinHeight, Math.Min(MaxDefaultHeight, work.Height));
        return (width, height);
    }

    private static void CenterOnWorkArea(AppWindow appWindow, DisplayArea display, int width, int height)
    {
        var work = display.WorkArea;
        appWindow.Move(new PointInt32(
            work.X + Math.Max(0, (work.Width - width) / 2),
            work.Y + Math.Max(0, (work.Height - height) / 2)));
    }

    private static void TrySetIcon(AppWindow appWindow)
    {
        try
        {
            appWindow.SetIcon("Assets/AppIcon.ico");
        }
        catch (Exception ex)
        {
            App.LogCrash("SetIcon", ex);
        }
    }
}
