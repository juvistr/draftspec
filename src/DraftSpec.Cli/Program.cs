using System.Security;
using DraftSpec.Cli;
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
    using var serviceProvider = services.BuildServiceProvider();

    // Load plugins from default directory
    var pluginLoader = serviceProvider.GetRequiredService<IPluginLoader>();
    var formatterRegistry = serviceProvider.GetRequiredService<ICliFormatterRegistry>();
    pluginLoader.RegisterFormatters(formatterRegistry);

    // Get command executor from factory
    var commandFactory = serviceProvider.GetRequiredService<ICommandFactory>();
    var executor = commandFactory.Create(options.Command);

    if (executor == null)
        return ShowUsage($"Unknown command: {options.Command}");

    var console = serviceProvider.GetRequiredService<IConsole>();

    try
    {
        return await executor(options, CancellationToken.None);
    }
    catch (ArgumentException ex)
    {
        console.WriteError($"Error: {ex.Message}");
        return 1;
    }
    catch (InvalidOperationException ex)
    {
        console.WriteError($"Error: {ex.Message}");
        return 1;
    }
    catch (SecurityException ex)
    {
        console.WriteError($"Error: {ex.Message}");
        return 1;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        // File system errors - show message without internal details
        console.WriteError($"Error: {ex.Message}");
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
        console.WriteError($"Error: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
#else
        console.WriteError("Error: An unexpected error occurred. Run with --help for usage information.");
#endif
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
    Console.WriteLine("  draftspec run <path> [options]   Run specs once and exit");
    Console.WriteLine("  draftspec watch <path> [options] Watch for changes and re-run");
    Console.WriteLine("  draftspec list <path> [options]  List specs without running them");
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
    Console.WriteLine("List Options:");
    Console.WriteLine("  --list-format <format>  Output format: tree (default), flat, json");
    Console.WriteLine("  --show-line-numbers     Show line numbers (default: true)");
    Console.WriteLine("  --no-line-numbers       Hide line numbers");
    Console.WriteLine("  --focused-only          Show only focused specs (fit)");
    Console.WriteLine("  --pending-only          Show only pending specs");
    Console.WriteLine("  --skipped-only          Show only skipped specs (xit)");
    Console.WriteLine();
    Console.WriteLine("Watch Options:");
    Console.WriteLine("  --incremental, -i       Only re-run changed specs (not entire files)");
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
    Console.WriteLine("  draftspec watch . --incremental");
    Console.WriteLine("  draftspec list . --list-format json -o specs.json");
    Console.WriteLine("  draftspec list . --focused-only");

    return error != null ? 1 : 0;
}