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

    var options = CliOptionsParser.Parse(args);

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

    var formatterRegistry = serviceProvider.GetRequiredService<ICliFormatterRegistry>();
    pluginLoader.RegisterFormatters(formatterRegistry);

    try
    {
        return options.Command switch
        {
            "run" => RunCommand.Execute(options, formatterRegistry),
            "watch" => await WatchCommand.ExecuteAsync(options),
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
    catch (Exception
#if DEBUG
        ex
#endif
    )
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
    Console.WriteLine("  --no-cache             Disable dotnet-script caching");
    Console.WriteLine();
    Console.WriteLine("Coverage Options:");
    Console.WriteLine("  --coverage             Enable code coverage collection");
    Console.WriteLine("  --coverage-output <dir>  Coverage output directory (default: ./coverage)");
    Console.WriteLine("  --coverage-format <fmt>  Coverage format: cobertura, xml (default: cobertura)");
    Console.WriteLine("  --coverage-report-formats <formats>  Additional report formats: html, json");
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
    Console.WriteLine("  draftspec run ./specs --coverage");
    Console.WriteLine("  draftspec watch .");

    return error != null ? 1 : 0;
}