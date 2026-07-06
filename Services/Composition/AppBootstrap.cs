using Microsoft.Extensions.DependencyInjection;
using WindowsPrinter.Services.Composition;
using WindowsPrinter.Views;

namespace WindowsPrinter.Services.Composition;

/// <summary>
/// Post-activation bootstrap: show window shell first, mount workspace UI on next UI tick.
/// </summary>
public static class AppBootstrap
{
    public static async Task LoadWorkspaceAsync(MainWindow window)
    {
        await Task.Yield();

        var session = App.Services.GetRequiredService<IWorkspaceSession>();
        var page = new PrintWorkspacePage(session.Workspace);
        window.MountWorkspace(page);

        try
        {
            await session.EnsureInitializedAsync();
        }
        catch (Exception ex)
        {
            App.LogCrash("WorkspaceInitialize", ex);
        }
    }
}
