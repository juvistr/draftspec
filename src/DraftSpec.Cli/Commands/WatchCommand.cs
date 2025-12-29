using DraftSpec.Cli.Configuration;
using DraftSpec.Formatters;

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
        var presenter = new ConsolePresenter(true);

        var cts = new CancellationTokenSource();

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

            var runner = new InProcessSpecRunner(
                options.FilterTags,
                options.ExcludeTags,
                options.FilterName,
                options.ExcludeName);

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
                        FormatConsoleOutput(result.Report),
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
            allSpecFiles = finder.FindSpecs(path);
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
        using var watcher = new FileWatcher(path, change =>
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

        Console.WriteLine();
        Console.WriteLine("Stopped watching.");

        return lastSummary?.Success == true ? 0 : 1;
    }

    private static string FormatConsoleOutput(SpecReport report)
    {
        var lines = new List<string>();

        void FormatContext(SpecContextReport ctx, int indent)
        {
            var prefix = new string(' ', indent * 2);
            lines.Add($"{prefix}{ctx.Description}");

            foreach (var spec in ctx.Specs)
            {
                var status = spec.Status switch
                {
                    "passed" => "✓",
                    "failed" => "✗",
                    "pending" => "○",
                    "skipped" => "-",
                    _ => "?"
                };
                lines.Add($"{prefix}  {status} {spec.Description}");
                if (!string.IsNullOrEmpty(spec.Error))
                {
                    lines.Add($"{prefix}    {spec.Error}");
                }
            }

            foreach (var child in ctx.Contexts)
            {
                FormatContext(child, indent + 1);
            }
        }

        foreach (var ctx in report.Contexts)
        {
            FormatContext(ctx, 0);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
