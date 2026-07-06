namespace WindowsPrinter.ViewModels;

public sealed record AddPathFailure(string Path, string Reason);

public sealed class AddPathsResult
{
    public int AddedCount { get; init; }

    public int DuplicateCount { get; init; }

    public IReadOnlyList<string> UnsupportedFiles { get; init; } = [];

    public IReadOnlyList<AddPathFailure> FailedFiles { get; init; } = [];
}
