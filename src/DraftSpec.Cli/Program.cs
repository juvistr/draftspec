using System.Security;
using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

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

    // Set up dependency injection
    var services = new ServiceCollection();
    services.AddDraftSpec();

    // Load plugins from default directory
    var pluginLoader = new PluginLoader();
    using var serviceProvider = services.BuildServiceProvider();

    var formatterRegistry = serviceProvider.GetRequiredService<IFormatterRegistry>();
    pluginLoader.RegisterFormatters(formatterRegistry);

    try
    {
        return options.Command switch
        {
            "run" => RunCommand.Execute(options, formatterRegistry),
            "watch" => await WatchCommand.ExecuteAsync(options.Path),
            "init" => InitCommand.Execute(options),
            "new" => NewCommand.Execute(options),
            _ => ShowUsage($"Unknown command: {options.Command}")
        };
    }
    catch (ArgumentException ex)
    {
        ShowError(ex.Message);
        return 1;
    }
    catch (SecurityException ex)
    {
        ShowError(ex.Message);
        return 1;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        // File system errors - show message without internal details
        ShowError(ex.Message);
        return 1;
    }
    catch (Exception ex)
    {
        // Unexpected errors - show details in debug mode
        #if DEBUG
        ShowError($"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        #else
        ShowError("An unexpected error occurred. Run with --help for usage information.");
        #endif
        return 1;
    }
}

static void ShowError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {message}");
    Console.ResetColor();
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
        else if (arg is "--parallel" or "-p")
        {
            options.Parallel = true;
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
    Console.WriteLine("  --parallel, -p         Run spec files in parallel");
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
