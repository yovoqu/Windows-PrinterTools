using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace WindowsPrinter.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "文件打印助手";

        var appWindow = AppWindow;
        if (appWindow is not null)
        {
            const int minWidth = 960;
            const int minHeight = 600;

            appWindow.Resize(new SizeInt32(1280, 800));
            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            if (appWindow.Presenter is OverlappedPresenter overlapped)
            {
                overlapped.IsResizable = true;
                overlapped.IsMinimizable = true;
                overlapped.IsMaximizable = true;
                overlapped.PreferredMinimumWidth = minWidth;
                overlapped.PreferredMinimumHeight = minHeight;
            }

            appWindow.Resize(new SizeInt32(
                Math.Max(appWindow.Size.Width, minWidth),
                Math.Max(appWindow.Size.Height, minHeight)));
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);
            }

            try
            {
                appWindow.SetIcon("Assets/AppIcon.ico");
            }
            catch (Exception ex)
            {
                App.LogCrash("SetIcon", ex);
            }

            var display = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
            if (display is not null)
            {
                var centered = appWindow.Position;
                centered.X = display.WorkArea.X + (display.WorkArea.Width - appWindow.Size.Width) / 2;
                centered.Y = display.WorkArea.Y + (display.WorkArea.Height - appWindow.Size.Height) / 2;
                appWindow.Move(centered);
            }
        }

        RootFrame.Navigate(typeof(PrintWorkspacePage));
    }
}
