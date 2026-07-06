using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using WindowsPrinter.Services;
using WindowsPrinter.Services.Abstractions;
using WindowsPrinter.Services.Storage;
using WindowsPrinter.Services.Discovery;
using WindowsPrinter.Services.Preferences;
using WindowsPrinter.Services.Logging;
using WindowsPrinter.Services.Printing;
using WindowsPrinter.Services.Printing.Handlers;
using WindowsPrinter.Services.Printing.Preview;
using WindowsPrinter.Services.Printing.Shell;
using WindowsPrinter.ViewModels;
using WindowsPrinter.Services.Composition;
using WindowsPrinter.Views;

namespace WindowsPrinter;

public partial class App : Application
{
    private static readonly string CrashLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        LocalAppStore.AppFolderName,
        "crash.log");

    public static MainWindow? MainWindow { get; private set; }

    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        Bootstrap.TryInitialize(0x00010002, out _);
        UnhandledException += (_, e) => LogCrash("UnhandledException", e.Exception);
        Services = ConfigureServices();
        InitializeComponent();
    }

    public static T GetRequiredService<T>() where T : notnull => Services.GetRequiredService<T>();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
            _ = AppBootstrap.LoadWorkspaceAsync(MainWindow);
        }
        catch (Exception ex)
        {
            LogCrash("OnLaunched", ex);
            throw;
        }
    }

    internal static void LogCrash(string source, Exception ex)
    {
        try
        {
            var dir = Path.GetDirectoryName(CrashLogPath)!;
            Directory.CreateDirectory(dir);
            File.AppendAllText(CrashLogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // ignore logging failures
        }
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILocalAppStore, LocalAppStore>();
        services.AddSingleton<IPrinterDiscoveryService, CompositePrinterDiscoveryService>();
        services.AddSingleton<IWindowHandleProvider, WindowHandleProvider>();
        services.AddSingleton<IAppDiagnostics, AppDiagnosticsService>();
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<IPrintSessionLog, PrintSessionLogService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<IUserPreferencesService, UserPreferencesService>();

        services.AddSingleton<IShellPrintCapabilityService, ShellPrintCapabilityService>();
        services.AddSingleton<IShellPrintService, ShellPrintService>();
        services.AddSingleton<IPrintPreviewService, PrintPreviewService>();

        services.AddSingleton<IFilePrintHandler, PdfPrintHandler>();
        services.AddSingleton<IFilePrintHandler, ImagePrintHandler>();
        services.AddSingleton<IFilePrintHandler, TextPrintHandler>();
        services.AddSingleton<IFilePrintHandler, ShellPrintHandlerAdapter>();
        services.AddSingleton<IPrintHandlerRegistry, PrintHandlerRegistry>();
        services.AddSingleton<IPrintFileItemFactory, PrintFileItemFactory>();
        services.AddSingleton<IPrintEnvironmentService, PrintEnvironmentService>();
        services.AddSingleton<IPrintOrchestrator, PrintOrchestrator>();
        services.AddSingleton<IPrintQueueService, PrintQueueService>();

        services.AddSingleton<PrintQueueViewModel>();
        services.AddSingleton<PrintSettingsViewModel>();
        services.AddSingleton<PrintLogViewModel>();
        services.AddSingleton<PrintWorkspaceViewModel>();
        services.AddSingleton<IWorkspaceSession, WorkspaceSession>();

        return services.BuildServiceProvider();
    }
}
