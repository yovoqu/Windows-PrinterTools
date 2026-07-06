using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using WindowsPrinter.Services;
using WindowsPrinter.Services.Abstractions;
using WindowsPrinter.Services.Discovery;
using WindowsPrinter.ViewModels;
using WindowsPrinter.Views;

namespace WindowsPrinter;

public partial class App : Application
{
    private static readonly string CrashLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowsPrinter",
        "crash.log");

    public static Window? MainWindow { get; private set; }

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
        services.AddSingleton<IPrinterDiscoveryService, CompositePrinterDiscoveryService>();
        services.AddSingleton<IPrintQueueService, PrintQueueService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddTransient<PrintSettingsViewModel>();
        services.AddTransient<PrintQueueViewModel>();
        services.AddTransient<PrintWorkspaceViewModel>();
        return services.BuildServiceProvider();
    }
}
