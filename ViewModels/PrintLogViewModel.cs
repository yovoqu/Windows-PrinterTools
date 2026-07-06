using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.System;
using WindowsPrinter.Models;
using WindowsPrinter.Services;
using WindowsPrinter.Services.Logging;

namespace WindowsPrinter.ViewModels;

public sealed partial class PrintLogViewModel : ObservableObject
{
    private readonly IPrintSessionLog _log;
    private readonly IUiDispatcher _dispatcher;
    private EventHandler? _changedHandler;
    private bool _attached;

    public PrintLogViewModel(IPrintSessionLog log, IUiDispatcher dispatcher)
    {
        _log = log;
        _dispatcher = dispatcher;
    }

    public ObservableCollection<PrintLogEntry> Entries { get; } = [];

    [ObservableProperty] private bool _isEmpty = true;

    public bool HasEntries => !IsEmpty;

    [ObservableProperty] private string _summaryText = "暂无日志";

    public void Attach()
    {
        if (_attached)
            return;

        _attached = true;
        _changedHandler = (_, _) => _dispatcher.RunOnUiThread(OnLogChanged);
        _log.Changed += _changedHandler;
        OnLogChanged();
    }

    public void Detach()
    {
        if (!_attached)
            return;

        _attached = false;
        if (_changedHandler is not null)
        {
            _log.Changed -= _changedHandler;
            _changedHandler = null;
        }
    }

    [RelayCommand]
    private void ClearLog() => _log.Clear();

    [RelayCommand]
    private async Task OpenLogFileAsync()
    {
        if (!File.Exists(_log.LogFilePath))
            return;

        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(_log.LogFilePath);
        await Launcher.LaunchFileAsync(file);
    }

    private void OnLogChanged()
    {
        var source = _log.Entries;
        if (source.Count == 0)
        {
            Entries.Clear();
            UpdateSummary();
            return;
        }

        if (source.Count < Entries.Count)
        {
            ReplaceAll(source);
            return;
        }

        if (source.Count == Entries.Count + 1)
        {
            Entries.Add(source[^1]);
            UpdateSummary();
            return;
        }

        if (source.Count != Entries.Count)
            ReplaceAll(source);
        else
            UpdateSummary();
    }

    private void ReplaceAll(IReadOnlyList<PrintLogEntry> source)
    {
        Entries.Clear();
        foreach (var entry in source)
            Entries.Add(entry);
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        IsEmpty = Entries.Count == 0;
        SummaryText = IsEmpty ? "暂无日志" : $"共 {Entries.Count} 条记录";
        OnPropertyChanged(nameof(HasEntries));
    }
}
