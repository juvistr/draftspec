using DraftSpec.Cli;

return await Run(args);

static async Task<int> Run(string[] args)
{
    if (args.Length == 0)
    {
        ShowUsage();
        return 1;
    }

    var command = args[0].ToLowerInvariant();
    var path = args.Length > 1 ? args[1] : ".";

    try
    {
        return command switch
        {
            "run" => RunSpecs(path),
            "watch" => await WatchSpecs(path),
            "--help" or "-h" or "help" => ShowUsage(),
            _ => ShowUsage($"Unknown command: {command}")
        };
    }
    catch (ArgumentException ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {ex.Message}");
        Console.ResetColor();
        return 1;
    }
}

static int ShowUsage(string? error = null)
{
    if (error != null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {error}");
        Console.ResetColor();
        Console.WriteLine();
    }

    Console.WriteLine("DraftSpec - RSpec-style testing for .NET");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  draftspec run <path>     Run specs once and exit");
    Console.WriteLine("  draftspec watch <path>   Watch for changes and re-run");
    Console.WriteLine();
    Console.WriteLine("Path can be:");
    Console.WriteLine("  - A directory (runs all *.spec.csx files recursively)");
    Console.WriteLine("  - A single .spec.csx file");
    Console.WriteLine("  - Omitted (defaults to current directory)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  draftspec run ./specs");
    Console.WriteLine("  draftspec watch .");
    Console.WriteLine("  draftspec run Calculator.spec.csx");

    return error != null ? 1 : 0;
}

static int RunSpecs(string path)
{
    var finder = new SpecFinder();
    var runner = new SpecRunner();
    var presenter = new ConsolePresenter(watchMode: false);

    var specFiles = finder.FindSpecs(path);
    presenter.ShowHeader(specFiles);

    var summary = runner.RunAll(specFiles);

    foreach (var result in summary.Results)
    {
        presenter.ShowResult(result, path);
    }

    presenter.ShowSummary(summary);

    return summary.Success ? 0 : 1;
}

static async Task<int> WatchSpecs(string path)
{
    var finder = new SpecFinder();
    var runner = new SpecRunner();
    var presenter = new ConsolePresenter(watchMode: true);

    var runRequested = new TaskCompletionSource();
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
