namespace WindowsPrinter.Services;

public interface IUiDispatcher
{
    void RunOnUiThread(Action action);
}

public sealed class UiDispatcher : IUiDispatcher
{
    public void RunOnUiThread(Action action)
    {
        var queue = App.MainWindow?.DispatcherQueue;
        if (queue is not null)
            queue.TryEnqueue(() => action());
        else
            action();
    }
}
