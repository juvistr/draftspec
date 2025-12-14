using DraftSpec.Cli;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Html;
using DraftSpec.Formatters.Markdown;

return await Run(args);

static async Task<int> Run(string[] args)
{
    if (args.Length == 0)
    {
        ShowUsage();
        return 1;
    }

    var options = ParseArgs(args);

    if (options.ShowHelp)
        return ShowUsage();

    if (options.Error != null)
        return ShowUsage(options.Error);

    try
    {
        return options.Command switch
        {
            "run" => RunSpecs(options),
            "watch" => await WatchSpecs(options.Path),
            _ => ShowUsage($"Unknown command: {options.Command}")
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

static CliOptions ParseArgs(string[] args)
{
    var options = new CliOptions();
    var positional = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        if (arg is "--help" or "-h" or "help")
        {
            options.ShowHelp = true;
        }
        else if (arg is "--format" or "-f")
        {
            if (i + 1 >= args.Length)
            {
                options.Error = "--format requires a value (console, json, markdown, html)";
                return options;
            }
            options.Format = args[++i].ToLowerInvariant();
        }
        else if (arg is "--output" or "-o")
        {
            if (i + 1 >= args.Length)
            {
                options.Error = "--output requires a file path";
                return options;
            }
            options.OutputFile = args[++i];
        }
        else if (arg == "--css-url")
        {
            if (i + 1 >= args.Length)
            {
                options.Error = "--css-url requires a URL";
                return options;
            }
            options.CssUrl = args[++i];
        }
        else if (!arg.StartsWith('-'))
        {
            positional.Add(arg);
        }
        else
        {
            options.Error = $"Unknown option: {arg}";
            return options;
        }
    }

    if (positional.Count > 0)
        options.Command = positional[0].ToLowerInvariant();
    if (positional.Count > 1)
        options.Path = positional[1];

    return options;
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
    Console.WriteLine("  draftspec run <path> [options]   Run specs once and exit");
    Console.WriteLine("  draftspec watch <path>           Watch for changes and re-run");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --format, -f <format>  Output format: console, json, markdown, html");
    Console.WriteLine("  --output, -o <file>    Write output to file instead of stdout");
    Console.WriteLine("  --css-url <url>        Custom CSS URL for HTML output");
    Console.WriteLine();
    Console.WriteLine("Path can be:");
    Console.WriteLine("  - A directory (runs all *.spec.csx files recursively)");
    Console.WriteLine("  - A single .spec.csx file");
    Console.WriteLine("  - Omitted (defaults to current directory)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  draftspec run ./specs");
    Console.WriteLine("  draftspec run ./specs --format markdown -o report.md");
    Console.WriteLine("  draftspec run ./specs --format html --css-url https://example.com/style.css");
    Console.WriteLine("  draftspec watch .");

    return error != null ? 1 : 0;
}

static int RunSpecs(CliOptions options)
{
    var finder = new SpecFinder();
    var runner = new SpecRunner();

    // For non-console formats, we need JSON output from specs
    var needsJson = options.Format is "json" or "markdown" or "html";

    if (!needsJson)
    {
        // Console output - use existing presenter
        var presenter = new ConsolePresenter(watchMode: false);
        runner.OnBuildStarted += presenter.ShowBuilding;
        runner.OnBuildCompleted += presenter.ShowBuildResult;

        var specFiles = finder.FindSpecs(options.Path);
        presenter.ShowHeader(specFiles);

        var summary = runner.RunAll(specFiles);

        presenter.ShowSpecsStarting();
        foreach (var result in summary.Results)
        {
            presenter.ShowResult(result, options.Path);
        }

        presenter.ShowSummary(summary);
        return summary.Success ? 0 : 1;
    }

    // JSON/Markdown/HTML - run with JSON output and format
    var specFilesForJson = finder.FindSpecs(options.Path);

    // Build projects first (silently for formatted output)
    foreach (var dir in specFilesForJson.Select(f => Path.GetDirectoryName(Path.GetFullPath(f))!).Distinct())
    {
        BuildProjects(dir);
    }

    // Run specs with JSON output and collect
    var jsonOutputs = new List<string>();
    var hasFailures = false;

    foreach (var specFile in specFilesForJson)
    {
        var result = runner.RunWithJson(specFile);
        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            jsonOutputs.Add(result.Output);
        }
        if (!result.Success)
        {
            hasFailures = true;
        }
    }

    // Combine JSON reports (for now, just use first one if single file)
    // TODO: Proper merging for multiple spec files
    var jsonReport = jsonOutputs.FirstOrDefault() ?? "{}";

    string output;
    if (options.Format == "json")
    {
        output = jsonReport;
    }
    else
    {
        var report = SpecReport.FromJson(jsonReport);
        report.Source = Path.GetFullPath(options.Path);
        IFormatter formatter = options.Format switch
        {
            "markdown" => new MarkdownFormatter(),
            "html" => new HtmlFormatter(new HtmlOptions
            {
                CssUrl = options.CssUrl ?? "https://cdnjs.cloudflare.com/ajax/libs/simpledotcss/2.3.7/simple.min.css"
            }),
            _ => throw new ArgumentException($"Unknown format: {options.Format}")
        };
        output = formatter.Format(report);
    }

    // Output to file or stdout
    if (!string.IsNullOrEmpty(options.OutputFile))
    {
        File.WriteAllText(options.OutputFile, output);
        Console.WriteLine($"Report written to {options.OutputFile}");
    }
    else
    {
        Console.WriteLine(output);
    }

    return hasFailures ? 1 : 0;
}

static void BuildProjects(string directory)
{
    var projects = Directory.GetFiles(directory, "*.csproj");
    foreach (var project in projects)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{project}\" --nologo -v q",
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi)!;
        process.WaitForExit();
    }
}

static async Task<int> WatchSpecs(string path)
{
    var finder = new SpecFinder();
    var runner = new SpecRunner();
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

class CliOptions
{
    public string Command { get; set; } = "";
    public string Path { get; set; } = ".";
    public string Format { get; set; } = "console";
    public string? OutputFile { get; set; }
    public string? CssUrl { get; set; }
    public bool ShowHelp { get; set; }
    public string? Error { get; set; }
}
