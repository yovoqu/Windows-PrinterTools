using System.ComponentModel;
using System.Runtime.CompilerServices;
using WindowsPrinter.Services.Printing;
using WindowsPrinter.Services.Printing.Handlers;

namespace WindowsPrinter.Models;

public sealed class PrintFileItem : INotifyPropertyChanged
{
    private bool _isSelected = true;
    private PrintJobStatus _status = PrintJobStatus.Pending;
    private string? _errorMessage;

    public PrintFileItem(string filePath, string? folderRoot = null)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        Extension = Path.GetExtension(filePath).ToLowerInvariant();
        RelativePathText = GetRelativePathText(filePath, folderRoot);
        DisplayPath = string.IsNullOrWhiteSpace(RelativePathText)
            ? Path.GetDirectoryName(filePath) ?? string.Empty
            : RelativePathText;

        long fileSize = 0;
        try
        {
            if (File.Exists(filePath))
                fileSize = new FileInfo(filePath).Length;
        }
        catch
        {
            // file may be temporarily unavailable
        }

        FileSizeBytes = fileSize;
        FileSizeText = FormatSize(fileSize);
        FileTypeText = GetFileTypeText(Extension);
        HandlerText = GetHandlerText(Extension);
    }

    public string FilePath { get; }
    public string FileName { get; }
    public string Extension { get; }
    public long FileSizeBytes { get; }
    public string FileSizeText { get; }
    public string FileTypeText { get; }
    public string HandlerText { get; }
    public string DisplayPath { get; }
    public string? RelativePathText { get; }
    public bool HasRelativePath => !string.IsNullOrEmpty(RelativePathText);

    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
    }

    public PrintJobStatus Status
    {
        get => _status;
        set { if (_status != value) { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); } }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { if (_errorMessage != value) { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); } }
    }

    public string StatusText => Status switch
    {
        PrintJobStatus.Pending => "等待",
        PrintJobStatus.Printing => "打印中",
        PrintJobStatus.Completed => "完成",
        PrintJobStatus.Failed => string.IsNullOrWhiteSpace(ErrorMessage) ? "失败" : $"失败：{ErrorMessage}",
        PrintJobStatus.Cancelled => "已取消",
        _ => "未知"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ResetStatus()
    {
        Status = PrintJobStatus.Pending;
        ErrorMessage = null;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var i = 0;
        while (size >= 1024 && i < units.Length - 1) { size /= 1024; i++; }
        return i == 0 ? $"{(int)size} {units[i]}" : $"{size:0.##} {units[i]}";
    }

    private static string GetFileTypeText(string extension) => extension switch
    {
        ".pdf" => "PDF 文档",
        ".txt" or ".log" or ".md" or ".csv" => "文本文件",
        ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tif" or ".tiff" or ".webp" => "图片",
        ".doc" or ".docx" => "Word 文档",
        ".xls" or ".xlsx" => "Excel 表格",
        ".ppt" or ".pptx" => "PowerPoint",
        ".rtf" => "RTF 文档",
        _ => "其他文件"
    };

    private static string GetHandlerText(string extension) => PrintHandlerFactory.GetHandlerKind(extension) switch
    {
        PrintHandlerKind.Pdf => "原生 PDF",
        PrintHandlerKind.Image => "图片渲染",
        PrintHandlerKind.Text => "文本渲染",
        PrintHandlerKind.Shell => "系统关联程序",
        _ => "未知"
    };

    private static string? GetRelativePathText(string filePath, string? folderRoot)
    {
        if (string.IsNullOrWhiteSpace(folderRoot))
        {
            return null;
        }

        var relative = Path.GetRelativePath(folderRoot, filePath);
        var fileName = Path.GetFileName(filePath);
        return string.Equals(relative, fileName, StringComparison.OrdinalIgnoreCase) ? null : relative;
    }
}
