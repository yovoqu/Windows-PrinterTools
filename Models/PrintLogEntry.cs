namespace WindowsPrinter.Models;

public sealed class PrintLogEntry
{
    public required DateTime Timestamp { get; init; }

    public required PrintLogLevel Level { get; init; }

    public required string Source { get; init; }

    public required string Message { get; init; }

    public string? FileName { get; init; }

    public string TimeText => Timestamp.ToString("HH:mm:ss");

    public string LevelText => Level switch
    {
        PrintLogLevel.Success => "成功",
        PrintLogLevel.Warning => "警告",
        PrintLogLevel.Error => "错误",
        _ => "信息"
    };
}
