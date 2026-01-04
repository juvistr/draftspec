using System.Text.RegularExpressions;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Services;
using DraftSpec.Cli.Watch;
using DraftSpec.Formatters;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Commands;

public class WatchCommand : ICommand<WatchOptions>
{
    private readonly ISpecFinder _specFinder;
    private readonly IInProcessSpecRunnerFactory _runnerFactory;
    private readonly IFileWatcherFactory _watcherFactory;
    private readonly IConsole _console;
    private readonly ISpecChangeTracker _changeTracker;
    private readonly IWatchEventProcessor _eventProcessor;

    public WatchCommand(
        ISpecFinder specFinder,
        IInProcessSpecRunnerFactory runnerFactory,
        IFileWatcherFactory watcherFactory,
        IConsole console,
        ISpecChangeTracker changeTracker,
        IWatchEventProcessor eventProcessor)
    {
        _specFinder = specFinder;
        _runnerFactory = runnerFactory;
        _watcherFactory = watcherFactory;
        _console = console;
        _changeTracker = changeTracker;
        _eventProcessor = eventProcessor;
    }

    public async Task<int> ExecuteAsync(WatchOptions options, CancellationToken ct = default)
    {
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
                options.Filter.FilterTags,
                options.Filter.ExcludeTags,
                options.Filter.FilterName,
                options.Filter.ExcludeName,
                options.Filter.FilterContext,
                options.Filter.ExcludeContext);

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

        // Helper to run only specific specs using filterName (for incremental mode)
        async Task RunFilteredSpecs(string specFile, string filterPattern)
        {
            presenter.Clear();

            var runner = _runnerFactory.Create(
                options.Filter.FilterTags,
                options.Filter.ExcludeTags,
                filterPattern, // Use the filter pattern for changed specs
                options.Filter.ExcludeName,
                options.Filter.FilterContext,
                options.Filter.ExcludeContext);

            runner.OnBuildStarted += presenter.ShowBuilding;
            runner.OnBuildCompleted += presenter.ShowBuildResult;
            runner.OnBuildSkipped += presenter.ShowBuildSkipped;

            try
            {
                presenter.ShowHeader([specFile], options.Parallel, isPartialRun: true);

                lastSummary = await runner.RunAllAsync([specFile], options.Parallel, cts.Token);

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
                throw;
            }
            catch (ArgumentException ex)
            {
                presenter.ShowError(ex.Message);
            }

            presenter.ShowWatching();
        }

        // Set up watcher with thin orchestration - decision logic is in IWatchEventProcessor
        using var watcher = _watcherFactory.Create(path, change =>
        {
            presenter.ShowRerunning();

            try
            {
                var action = _eventProcessor.ProcessChangeAsync(
                    change,
                    allSpecFiles ?? [],
                    path,
                    options.Incremental,
                    options.NoCache,
                    cts.Token).GetAwaiter().GetResult();

                switch (action.Type)
                {
                    case WatchActionType.Skip:
                        if (action.Message != null)
                            _console.WriteLine(action.Message);
                        presenter.ShowWatching();
                        break;

                    case WatchActionType.RunAll:
                        RunAll().GetAwaiter().GetResult();
                        break;

                    case WatchActionType.RunFile:
                        if (action.Message != null)
                            _console.WriteLine(action.Message);
                        RunSpecs([action.FilePath!], isPartialRun: true).GetAwaiter().GetResult();
                        break;

                    case WatchActionType.RunFiltered:
                        _console.WriteLine(action.Message!);
                        RunFilteredSpecs(action.FilePath!, action.FilterPattern!).GetAwaiter().GetResult();
                        if (action.ParseResultToRecord != null)
                            _changeTracker.RecordState(action.FilePath!, action.ParseResultToRecord);
                        break;
                }
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

    /// <summary>
    /// Builds a regex pattern that matches any of the spec descriptions.
    /// </summary>
    internal static string BuildFilterPattern(IReadOnlyList<SpecChange> specs)
    {
        if (specs.Count == 0)
            return "^$"; // Match nothing

        // Build regex that matches any of the spec descriptions
        // Use Regex.Escape() for special characters
        var escaped = specs.Select(s => Regex.Escape(s.Description));
        return $"^({string.Join("|", escaped)})$";
    }
}
