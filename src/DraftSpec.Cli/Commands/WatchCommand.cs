using DraftSpec.Cli.Configuration;
using DraftSpec.Cli.Coverage;

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

        // Initialize coverage runner if enabled
        CoverageRunner? coverageRunner = null;
        if (options.Coverage)
        {
            if (!CoverageToolDetector.IsAvailable)
            {
                Console.Error.WriteLine("Error: dotnet-coverage tool is not installed.");
                Console.Error.WriteLine("Install with: dotnet tool install -g dotnet-coverage");
                return 1;
            }

            var coverageOutput = Path.GetFullPath(options.CoverageOutput ?? "./coverage");
            coverageRunner = new CoverageRunner(coverageOutput, options.CoverageFormat);
        }

        var path = options.Path;
        var finder = new SpecFinder();
        var runner = new SpecFileRunner(
            options.NoCache,
            options.FilterTags,
            options.ExcludeTags,
            options.FilterName,
            options.ExcludeName,
            coverageRunner);
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
                presenter.ShowHeader(specFiles, options.Parallel, isPartialRun);

                lastSummary = runner.RunAll(specFiles, options.Parallel);

                presenter.ShowSpecsStarting();
                foreach (var result in lastSummary.Results) presenter.ShowResult(result, path);

                presenter.ShowSummary(lastSummary);

                // Handle coverage after each run (warnings only, don't exit)
                HandleCoverageInWatchMode(coverageRunner, configResult.Config, options, presenter);
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

    /// <summary>
    /// Handle coverage in watch mode - merge files, generate reports, show warnings.
    /// Unlike run mode, this doesn't exit on threshold failure.
    /// </summary>
    private static void HandleCoverageInWatchMode(
        CoverageRunner? coverageRunner,
        DraftSpecProjectConfig? config,
        CliOptions options,
        ConsolePresenter presenter)
    {
        if (coverageRunner == null)
            return;

        var mergedFile = coverageRunner.MergeCoverageFiles();
        if (mergedFile == null)
            return;

        presenter.ShowCoverageReport(mergedFile);
        coverageRunner.CleanupIntermediateFiles();

        // Generate additional report formats if requested
        var reportFormatsStr = options.CoverageReportFormats;
        var reportFormats = !string.IsNullOrEmpty(reportFormatsStr)
            ? reportFormatsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : config?.Coverage?.ReportFormats;

        if (reportFormats is { Count: > 0 })
        {
            var generatedReports = coverageRunner.GenerateReports(mergedFile, reportFormats);
            foreach (var (format, path) in generatedReports)
            {
                presenter.ShowCoverageReportGenerated(format, path);
            }
        }

        // Check thresholds and show warnings (don't exit)
        var thresholds = config?.Coverage?.Thresholds;
        if (thresholds != null && (thresholds.Line.HasValue || thresholds.Branch.HasValue))
        {
            var checker = new CoverageThresholdChecker();
            var result = checker.CheckFile(mergedFile, thresholds);

            if (result != null)
            {
                presenter.ShowCoverageSummary(result.ActualLinePercent, result.ActualBranchPercent);

                if (!result.Passed)
                {
                    presenter.ShowCoverageThresholdWarnings(result.Failures);
                }
            }
        }
    }
}