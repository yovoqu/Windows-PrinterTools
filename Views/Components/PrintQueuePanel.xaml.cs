using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using WindowsPrinter.ViewModels;

namespace WindowsPrinter.Views.Components;

public sealed partial class PrintQueuePanel : UserControl
{
    public PrintQueuePanel()
    {
        InitializeComponent();
        AllowDrop = true;
        DragOver += OnDragOver;
        Drop += OnDropAsync;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // TableView captures drag-drop when the queue is visible — wire the same handlers.
        QueueTable.AllowDrop = true;
        QueueTable.DragOver += OnDragOver;
        QueueTable.Drop += OnDropAsync;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (DataContext is PrintQueueViewModel { IsPrinting: true })
        {
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.Caption = "添加到打印队列";
    }

    private async void OnDropAsync(object sender, DragEventArgs e)
    {
        if (DataContext is not PrintQueueViewModel viewModel) return;

        try
        {
            if (viewModel.IsPrinting)
            {
                viewModel.ReportOperationError("打印进行中，无法拖放添加文件。");
                return;
            }

            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                viewModel.ReportOperationError("拖放的内容无法识别，请拖入文件或文件夹。");
                return;
            }

            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count == 0)
            {
                viewModel.ReportOperationError("拖放的内容为空，未添加任何文件。");
                return;
            }

            await viewModel.AddDroppedPathsAsync(items.Select(item => item.Path));
        }
        catch (Exception)
        {
            viewModel.ReportOperationError("拖放文件时发生错误，请重试。");
        }
    }
}
