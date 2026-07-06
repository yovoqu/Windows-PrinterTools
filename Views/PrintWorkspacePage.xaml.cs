using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WindowsPrinter.ViewModels;

namespace WindowsPrinter.Views;

public sealed partial class PrintWorkspacePage : Page
{
    public PrintWorkspaceViewModel ViewModel { get; }

    public PrintWorkspacePage()
    {
        ViewModel = App.GetRequiredService<PrintWorkspaceViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        DataContext = ViewModel;
    }
}
