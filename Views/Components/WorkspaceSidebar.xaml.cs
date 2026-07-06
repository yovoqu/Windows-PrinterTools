using Microsoft.UI.Xaml.Controls;
using WindowsPrinter.ViewModels;

namespace WindowsPrinter.Views.Components;

public sealed partial class WorkspaceSidebar : UserControl
{
    private bool _logPanelLoaded;

    public WorkspaceSidebar()
    {
        InitializeComponent();
    }

    private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_logPanelLoaded || !ReferenceEquals(SidebarTabs.SelectedItem, LogTab))
            return;

        if (DataContext is not PrintWorkspaceViewModel workspace)
            return;

        _logPanelLoaded = true;
        LogHost.Children.Add(new PrintLogPanel
        {
            DataContext = workspace.Log,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch
        });
    }
}
