using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowsPrinter.Models;
using WindowsPrinter.Services.Abstractions;
using WindowsPrinter.Services.Discovery;
using WindowsPrinter.Services.Preferences;

namespace WindowsPrinter.ViewModels;

public sealed record DuplexOption(PrintDuplexMode Mode, string Label);

public sealed partial class PrintSettingsViewModel : ObservableObject
{
    private readonly IPrinterDiscoveryService _printerDiscovery;
    private readonly IUserPreferencesService _preferences;
    private readonly IAppDiagnostics _diagnostics;
    private UserPreferences _loadedPreferences = new();
    private bool _suppressPreferenceSave;

    public PrintSettingsViewModel(
        IPrinterDiscoveryService printerDiscovery,
        IUserPreferencesService preferences,
        IAppDiagnostics diagnostics)
    {
        _printerDiscovery = printerDiscovery;
        _preferences = preferences;
        _diagnostics = diagnostics;
        _selectedDuplexOption = DuplexOptions.First(o => o.Mode == PrintDuplexMode.Simplex);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _loadedPreferences = _preferences.Load();

        _suppressPreferenceSave = true;
        try
        {
            UseColor = _loadedPreferences.UseColor;
            Copies = _loadedPreferences.Copies;
            SelectedDuplexOption = DuplexOptions.First(o => o.Mode == _loadedPreferences.Duplex);
            ShellSpoolerTimeoutSeconds = _loadedPreferences.ShellSpoolerTimeoutSeconds;
        }
        finally
        {
            _suppressPreferenceSave = false;
        }

        await RefreshPrintersAsync();
    }

    public ObservableCollection<PrinterInfo> Printers { get; } = [];

    public IReadOnlyList<DuplexOption> DuplexOptions { get; } =
    [
        new(PrintDuplexMode.Simplex, "单面"),
        new(PrintDuplexMode.LongEdge, "双面（长边翻转）"),
        new(PrintDuplexMode.ShortEdge, "双面（短边翻转）")
    ];

    [ObservableProperty] private PrinterInfo? _selectedPrinter;
    [ObservableProperty] private bool _useColor = true;
    [ObservableProperty] private int _copies = 1;
    [ObservableProperty] private DuplexOption? _selectedDuplexOption;
    [ObservableProperty] private int _shellSpoolerTimeoutSeconds = 60;
    [ObservableProperty] private bool _isLoadingPrinters;
    [ObservableProperty] private string? _printerLoadError;

    public bool IsPrinterComboEnabled => !IsLoadingPrinters;

    public bool HasPrinterLoadError => !string.IsNullOrWhiteSpace(PrinterLoadError);

    public string? SelectedPrinterName => SelectedPrinter?.Name;

    partial void OnIsLoadingPrintersChanged(bool value) => OnPropertyChanged(nameof(IsPrinterComboEnabled));

    partial void OnPrinterLoadErrorChanged(string? value) => OnPropertyChanged(nameof(HasPrinterLoadError));

    partial void OnSelectedPrinterChanged(PrinterInfo? value)
    {
        if (!_suppressPreferenceSave)
            SavePreferences();
        PrinterSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnUseColorChanged(bool value)
    {
        if (!_suppressPreferenceSave)
            SavePreferences();
    }

    partial void OnCopiesChanged(int value)
    {
        if (!_suppressPreferenceSave)
            SavePreferences();
    }

    partial void OnSelectedDuplexOptionChanged(DuplexOption? value)
    {
        if (!_suppressPreferenceSave)
            SavePreferences();
    }

    partial void OnShellSpoolerTimeoutSecondsChanged(int value)
    {
        if (!_suppressPreferenceSave)
            SavePreferences();
    }

    public event EventHandler? PrinterSelectionChanged;

    public PrintSettings BuildPrintSettings()
    {
        if (string.IsNullOrWhiteSpace(SelectedPrinterName))
            throw new InvalidOperationException("请选择打印机。");

        return new PrintSettings
        {
            PrinterName = SelectedPrinterName,
            UseColor = UseColor,
            Copies = (short)Math.Clamp(Copies, 1, 99),
            Duplex = SelectedDuplexOption?.Mode ?? PrintDuplexMode.Simplex,
            ShellSpoolerTimeoutSeconds = Math.Clamp(ShellSpoolerTimeoutSeconds, 15, 300)
        };
    }

    [RelayCommand]
    private async Task RefreshPrintersAsync()
    {
        if (IsLoadingPrinters) return;

        IsLoadingPrinters = true;
        PrinterLoadError = null;
        var currentName = SelectedPrinter?.Name ?? _loadedPreferences.LastPrinterName;

        try
        {
            var discovered = await _printerDiscovery.DiscoverAsync();
            Printers.Clear();
            foreach (var printer in discovered)
                Printers.Add(printer);

            if (Printers.Count == 0)
            {
                _suppressPreferenceSave = true;
                try { SelectedPrinter = null; }
                finally { _suppressPreferenceSave = false; }

                PrinterLoadError = "未检测到可用打印机，请检查系统打印机设置后点击刷新。";
                return;
            }

            _suppressPreferenceSave = true;
            try
            {
                SelectedPrinter =
                    Printers.FirstOrDefault(p => string.Equals(p.Name, currentName, StringComparison.OrdinalIgnoreCase))
                    ?? Printers.FirstOrDefault(p => p.IsDefault)
                    ?? Printers[0];
            }
            finally
            {
                _suppressPreferenceSave = false;
            }
        }
        catch (Exception ex)
        {
            _suppressPreferenceSave = true;
            try { SelectedPrinter = null; }
            finally { _suppressPreferenceSave = false; }

            PrinterLoadError = $"加载打印机失败：{ex.Message}";
            _diagnostics.LogCrash("RefreshPrinters", ex);
        }
        finally
        {
            IsLoadingPrinters = false;
        }
    }

    private void SavePreferences()
    {
        _preferences.Save(new UserPreferences
        {
            LastPrinterName = SelectedPrinter?.Name,
            UseColor = UseColor,
            Copies = Copies,
            Duplex = SelectedDuplexOption?.Mode ?? PrintDuplexMode.Simplex,
            ShellSpoolerTimeoutSeconds = ShellSpoolerTimeoutSeconds
        });
    }
}
