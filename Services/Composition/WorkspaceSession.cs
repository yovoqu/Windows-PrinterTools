using WindowsPrinter.ViewModels;

namespace WindowsPrinter.Services.Composition;

public sealed class WorkspaceSession : IWorkspaceSession
{
    private int _initialized;

    public WorkspaceSession(PrintWorkspaceViewModel workspace) => Workspace = workspace;

    public PrintWorkspaceViewModel Workspace { get; }

    public async ValueTask EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            return;

        Workspace.Log.Attach();
        await Workspace.Settings.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }
}
