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
            "init" => InitSpecs(options),
            "new" => NewSpec(options),
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
        else if (arg == "--force")
        {
            options.Force = true;
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
    {
        // For 'new' command, second arg is the spec name
        if (options.Command == "new")
            options.SpecName = positional[1];
        else
            options.Path = positional[1];
    }
    if (positional.Count > 2 && options.Command == "new")
        options.Path = positional[2];

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
    Console.WriteLine("  draftspec init [path]            Initialize spec infrastructure");
    Console.WriteLine("  draftspec new <name> [path]      Create a new spec file");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --format, -f <format>  Output format: console, json, markdown, html");
    Console.WriteLine("  --output, -o <file>    Write output to file instead of stdout");
    Console.WriteLine("  --css-url <url>        Custom CSS URL for HTML output");
    Console.WriteLine("  --force                Overwrite existing files (for init)");
    Console.WriteLine();
    Console.WriteLine("Path can be:");
    Console.WriteLine("  - A directory (runs all *.spec.csx files recursively)");
    Console.WriteLine("  - A single .spec.csx file");
    Console.WriteLine("  - Omitted (defaults to current directory)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  draftspec init");
    Console.WriteLine("  draftspec new Calculator");
    Console.WriteLine("  draftspec run ./specs");
    Console.WriteLine("  draftspec run ./specs --format markdown -o report.md");
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

static int InitSpecs(CliOptions options)
{
    var resolver = new ProjectResolver();
    var directory = Path.GetFullPath(options.Path);

    if (!Directory.Exists(directory))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Directory not found: {directory}");
        Console.ResetColor();
        return 1;
    }

    // Find project
    var csproj = resolver.FindProject(directory);
    ProjectResolver.ProjectInfo? info = null;

    if (csproj == null)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("No .csproj found. Creating spec_helper without project reference.");
        Console.ResetColor();
    }
    else
    {
        info = resolver.GetProjectInfo(csproj);
        if (info == null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Could not get project info for {Path.GetFileName(csproj)}");
            Console.ResetColor();
        }
    }

    // Generate spec_helper.csx
    var specHelperPath = Path.Combine(directory, "spec_helper.csx");
    if (File.Exists(specHelperPath) && !options.Force)
    {
        Console.WriteLine("spec_helper.csx already exists (use --force to overwrite)");
    }
    else
    {
        var specHelper = GenerateSpecHelper(info, directory);
        File.WriteAllText(specHelperPath, specHelper);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Created spec_helper.csx");
        Console.ResetColor();
    }

    // Generate omnisharp.json
    var omnisharpPath = Path.Combine(directory, "omnisharp.json");
    if (File.Exists(omnisharpPath) && !options.Force)
    {
        Console.WriteLine("omnisharp.json already exists (use --force to overwrite)");
    }
    else
    {
        var omnisharp = GenerateOmnisharp(info?.TargetFramework ?? "net10.0");
        File.WriteAllText(omnisharpPath, omnisharp);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Created omnisharp.json");
        Console.ResetColor();
    }

    return 0;
}

static string GenerateSpecHelper(ProjectResolver.ProjectInfo? info, string directory)
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("#r \"nuget: DraftSpec\"");

    if (info != null)
    {
        // Make the path relative to the directory
        var relativePath = Path.GetRelativePath(directory, info.TargetPath);
        sb.AppendLine($"#r \"{relativePath}\"");
    }

    sb.AppendLine();
    sb.AppendLine("using static DraftSpec.Dsl;");
    sb.AppendLine();
    sb.AppendLine("// Add shared fixtures below:");

    return sb.ToString();
}

static string GenerateOmnisharp(string targetFramework)
{
    return $$"""
        {
          "script": {
            "enableScriptNuGetReferences": true,
            "defaultTargetFramework": "{{targetFramework}}"
          }
        }
        """;
}

static int NewSpec(CliOptions options)
{
    var name = options.SpecName;
    if (string.IsNullOrEmpty(name))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Usage: draftspec new <Name>");
        Console.ResetColor();
        return 1;
    }

    var directory = Path.GetFullPath(options.Path);

    if (!Directory.Exists(directory))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Directory not found: {directory}");
        Console.ResetColor();
        return 1;
    }

    var specHelperPath = Path.Combine(directory, "spec_helper.csx");
    if (!File.Exists(specHelperPath))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Warning: spec_helper.csx not found. Run 'draftspec init' first.");
        Console.ResetColor();
    }

    var specPath = Path.Combine(directory, $"{name}.spec.csx");
    if (File.Exists(specPath))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{name}.spec.csx already exists");
        Console.ResetColor();
        return 1;
    }

    var specContent = GenerateSpec(name);
    File.WriteAllText(specPath, specContent);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Created {name}.spec.csx");
    Console.ResetColor();

    return 0;
}

static string GenerateSpec(string name)
{
    return $$"""
        #load "spec_helper.csx"
        using static DraftSpec.Dsl;

        describe("{{name}}", () => {
            it("works", () => pending());
        });

        run();
        """;
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
    public bool Force { get; set; }
    public string? SpecName { get; set; }
}
