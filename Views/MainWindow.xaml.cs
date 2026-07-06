using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WindowsPrinter.Infrastructure;

namespace WindowsPrinter.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "文件打印助手";
        WindowPlacement.Apply(this);
        Activated += OnWindowActivated;
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
            return;

        WindowPlacement.EnsureDefaultSize(this);
    }

    public void MountWorkspace(FrameworkElement workspaceRoot)
    {
        workspaceRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
        workspaceRoot.VerticalAlignment = VerticalAlignment.Stretch;

        var host = RootHost;
        if (host.Children.Count > 1)
            host.Children.RemoveAt(1);

        Grid.SetRow(workspaceRoot, 1);
        host.Children.Add(workspaceRoot);
        StartupRing.IsActive = false;
        StartupRing.Visibility = Visibility.Collapsed;

        WindowPlacement.EnsureDefaultSize(this);
    }
}
