using DraftSpec.Cli.Configuration;

namespace DraftSpec.Cli.Commands;

public static class WatchCommand
{
    public static async Task<int> ExecuteAsync(CliOptions options)
    {
        // Load project configuration from draftspec.json
        var configResult = ConfigLoader.Load(options.Path);
        if (configResult.Error != null)
        {
            Console.Error.WriteLine($"Error: {configResult.Error}");
            return 1;
        }

        if (configResult.Config != null)
            options.ApplyDefaults(configResult.Config);

        var path = options.Path;
        var finder = new SpecFinder();
        var runner = new SpecFileRunner(
            options.NoCache,
            options.FilterTags,
            options.ExcludeTags,
            options.FilterName,
            options.ExcludeName);
        var presenter = new ConsolePresenter(true);

        runner.OnBuildStarted += presenter.ShowBuilding;
        runner.OnBuildCompleted += presenter.ShowBuildResult;
        runner.OnBuildSkipped += presenter.ShowBuildSkipped;

        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        RunSummary? lastSummary = null;
        IReadOnlyList<string>? allSpecFiles = null;

        void RunSpecs(IReadOnlyList<string> specFiles, bool isPartialRun = false)
        {
            presenter.Clear();

            try
            {
                presenter.ShowHeader(specFiles, isPartialRun: isPartialRun);

                lastSummary = runner.RunAll(specFiles, options.Parallel);

                presenter.ShowSpecsStarting();
                foreach (var result in lastSummary.Results) presenter.ShowResult(result, path);

                presenter.ShowSummary(lastSummary);
            }
            catch (ArgumentException ex)
            {
                presenter.ShowError(ex.Message);
            }

            presenter.ShowWatching();
        }

        void RunAll()
        {
            allSpecFiles = finder.FindSpecs(path);
            RunSpecs(allSpecFiles);
        }

        // Initial run
        RunAll();

        // Set up watcher
        using var watcher = new FileWatcher(path, change =>
        {
            presenter.ShowRerunning();

            // Selective re-run: if only one spec file changed, run just that one
            if (change.IsSpecFile && change.FilePath != null)
            {
                // Verify the changed file is in our spec list
                var changedSpec = allSpecFiles?.FirstOrDefault(f =>
                    string.Equals(Path.GetFullPath(f), change.FilePath, StringComparison.OrdinalIgnoreCase));

                if (changedSpec != null)
                {
                    RunSpecs([changedSpec], true);
                    return;
                }
            }

            // Full run: source file changed, multiple files changed, or file not in list
            RunAll();
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