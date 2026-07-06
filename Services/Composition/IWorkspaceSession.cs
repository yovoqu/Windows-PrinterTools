using WindowsPrinter.ViewModels;

namespace WindowsPrinter.Services.Composition;

public interface IWorkspaceSession
{
    PrintWorkspaceViewModel Workspace { get; }

    ValueTask EnsureInitializedAsync(CancellationToken cancellationToken = default);
}
