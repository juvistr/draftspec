namespace DraftSpec.Cli.Commands;

public static class WatchCommand
{
    public static async Task<int> ExecuteAsync(string path)
    {
        var finder = new SpecFinder();
        var runner = new SpecFileRunner();
        var presenter = new ConsolePresenter(watchMode: true);

        runner.OnBuildStarted += presenter.ShowBuilding;
        runner.OnBuildCompleted += presenter.ShowBuildResult;

        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        RunSummary? lastSummary = null;

        void RunOnce()
        {
            presenter.Clear();

            try
            {
                var specFiles = finder.FindSpecs(path);
                presenter.ShowHeader(specFiles);

                lastSummary = runner.RunAll(specFiles);

                presenter.ShowSpecsStarting();
                foreach (var result in lastSummary.Results)
                {
                    presenter.ShowResult(result, path);
                }

                presenter.ShowSummary(lastSummary);
            }
            catch (ArgumentException ex)
            {
                presenter.ShowError(ex.Message);
            }

            presenter.ShowWatching();
        }

        // Initial run
        RunOnce();

        // Set up watcher
        using var watcher = new FileWatcher(path, () =>
        {
            presenter.ShowRerunning();
            RunOnce();
        });

        // Wait for cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // Normal exit via Ctrl+C
        }

        Console.WriteLine();
        Console.WriteLine("Stopped watching.");

        return lastSummary?.Success == true ? 0 : 1;
    }
}
