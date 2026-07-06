using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowsPrinter.Models;
using WindowsPrinter.Services.Discovery;

namespace WindowsPrinter.ViewModels;

public sealed partial class PrintSettingsViewModel : ObservableObject
{
    private readonly IPrinterDiscoveryService _printerDiscovery;

    public PrintSettingsViewModel(IPrinterDiscoveryService printerDiscovery)
    {
        _printerDiscovery = printerDiscovery;
        _ = RefreshPrintersAsync();
    }

    public ObservableCollection<PrinterInfo> Printers { get; } = [];

    [ObservableProperty] private PrinterInfo? _selectedPrinter;
    [ObservableProperty] private bool _useColor = true;
    [ObservableProperty] private bool _isLoadingPrinters;
    [ObservableProperty] private string? _printerLoadError;

    public bool IsPrinterComboEnabled => !IsLoadingPrinters;

    public bool HasPrinterLoadError => !string.IsNullOrWhiteSpace(PrinterLoadError);

    public string? SelectedPrinterName => SelectedPrinter?.Name;

    partial void OnIsLoadingPrintersChanged(bool value) => OnPropertyChanged(nameof(IsPrinterComboEnabled));

    partial void OnPrinterLoadErrorChanged(string? value) => OnPropertyChanged(nameof(HasPrinterLoadError));

    partial void OnSelectedPrinterChanged(PrinterInfo? value) =>
        PrinterSelectionChanged?.Invoke(this, EventArgs.Empty);

    public event EventHandler? PrinterSelectionChanged;

    [RelayCommand]
    private async Task RefreshPrintersAsync()
    {
        if (IsLoadingPrinters) return;

        IsLoadingPrinters = true;
        PrinterLoadError = null;
        var currentName = SelectedPrinter?.Name;

        try
        {
            var discovered = await _printerDiscovery.DiscoverAsync();
            Printers.Clear();
            foreach (var printer in discovered)
                Printers.Add(printer);

            if (Printers.Count == 0)
            {
                SelectedPrinter = null;
                PrinterLoadError = "未检测到可用打印机，请检查系统打印机设置后点击刷新。";
                return;
            }

            SelectedPrinter =
                Printers.FirstOrDefault(p => string.Equals(p.Name, currentName, StringComparison.OrdinalIgnoreCase))
                ?? Printers.FirstOrDefault(p => p.IsDefault)
                ?? Printers[0];
        }
        catch (Exception ex)
        {
            SelectedPrinter = null;
            PrinterLoadError = $"加载打印机失败：{ex.Message}";
            App.LogCrash("RefreshPrinters", ex);
        }
        finally
        {
            IsLoadingPrinters = false;
        }
    }
}
