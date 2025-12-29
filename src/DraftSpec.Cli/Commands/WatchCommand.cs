using DraftSpec.Cli.Services;
using DraftSpec.Formatters;

namespace DraftSpec.Cli.Commands;

public class WatchCommand : ICommand
{
    private readonly ISpecFinder _specFinder;
    private readonly IInProcessSpecRunnerFactory _runnerFactory;
    private readonly IFileWatcherFactory _watcherFactory;
    private readonly IConsole _console;
    private readonly IConfigLoader _configLoader;

    public WatchCommand(
        ISpecFinder specFinder,
        IInProcessSpecRunnerFactory runnerFactory,
        IFileWatcherFactory watcherFactory,
        IConsole console,
        IConfigLoader configLoader)
    {
        _specFinder = specFinder;
        _runnerFactory = runnerFactory;
        _watcherFactory = watcherFactory;
        _console = console;
        _configLoader = configLoader;
    }

    public async Task<int> ExecuteAsync(CliOptions options, CancellationToken ct = default)
    {
        // Load project configuration from draftspec.json
        var configResult = _configLoader.Load(options.Path);
        if (configResult.Error != null)
            throw new InvalidOperationException(configResult.Error);

        if (configResult.Config != null)
            options.ApplyDefaults(configResult.Config);

        var path = options.Path;
        var presenter = new ConsolePresenter(_console, watchMode: true);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        InProcessRunSummary? lastSummary = null;
        IReadOnlyList<string>? allSpecFiles = null;

        async Task RunSpecs(IReadOnlyList<string> specFiles, bool isPartialRun = false)
        {
            presenter.Clear();

            var runner = _runnerFactory.Create(
                options.FilterTags,
                options.ExcludeTags,
                options.FilterName,
                options.ExcludeName,
                options.FilterContext,
                options.ExcludeContext);

            runner.OnBuildStarted += presenter.ShowBuilding;
            runner.OnBuildCompleted += presenter.ShowBuildResult;
            runner.OnBuildSkipped += presenter.ShowBuildSkipped;

            try
            {
                presenter.ShowHeader(specFiles, options.Parallel, isPartialRun);

                lastSummary = await runner.RunAllAsync(specFiles, options.Parallel, cts.Token);

                presenter.ShowSpecsStarting();

                foreach (var result in lastSummary.Results)
                {
                    var legacyResult = new SpecRunResult(
                        result.SpecFile,
                        ConsoleOutputFormatter.Format(result.Report),
                        result.Error?.Message ?? "",
                        result.Success ? 0 : 1,
                        result.Duration);

                    presenter.ShowResult(legacyResult, path);
                }

                // Convert to legacy summary for presenter
                var legacySummary = new RunSummary(
                    lastSummary.Results.Select(r => new SpecRunResult(
                        r.SpecFile,
                        "",
                        r.Error?.Message ?? "",
                        r.Success ? 0 : 1,
                        r.Duration)).ToList(),
                    lastSummary.TotalDuration);

                presenter.ShowSummary(legacySummary);
            }
            catch (OperationCanceledException)
            {
                // Watch was cancelled
                throw;
            }
            catch (ArgumentException ex)
            {
                presenter.ShowError(ex.Message);
            }

            presenter.ShowWatching();
        }

        async Task RunAll()
        {
            allSpecFiles = _specFinder.FindSpecs(path);
            await RunSpecs(allSpecFiles);
        }

        // Initial run
        try
        {
            await RunAll();
        }
        catch (OperationCanceledException)
        {
            // Exit immediately if cancelled during initial run
            return lastSummary?.Success == true ? 0 : 1;
        }

        // Set up watcher
        using var watcher = _watcherFactory.Create(path, change =>
        {
            presenter.ShowRerunning();

            try
            {
                // Selective re-run: if only one spec file changed, run just that one
                if (change.IsSpecFile && change.FilePath != null)
                {
                    var changedSpec = allSpecFiles?.FirstOrDefault(f =>
                        string.Equals(Path.GetFullPath(f), change.FilePath, StringComparison.OrdinalIgnoreCase));

                    if (changedSpec != null)
                    {
                        RunSpecs([changedSpec], true).GetAwaiter().GetResult();
                        return;
                    }
                }

                // Full run: source file changed, multiple files changed, or file not in list
                RunAll().GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Ignore - watcher will stop
            }
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

        _console.WriteLine();
        _console.WriteLine("Stopped watching.");

        return lastSummary?.Success == true ? 0 : 1;
    }
}
