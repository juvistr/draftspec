using System.Security;
using DraftSpec.Cli;
using DraftSpec.Cli.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

return await Run(args).ConfigureAwait(false);

static async Task<int> Run(string[] args)
{
    // For no-args and early errors, create a simple UsageWriter directly
    // to avoid setting up full DI just to show help
    var simpleUsageWriter = new UsageWriter(new SystemConsole());

    if (args.Length == 0)
        return simpleUsageWriter.Show();

    var options = CliOptionsParser.Parse(args);

    if (options.ShowHelp)
        return simpleUsageWriter.Show();

    if (options.Error != null)
        return simpleUsageWriter.Show(options.Error);

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
    {
        var usageWriter = serviceProvider.GetRequiredService<IUsageWriter>();
        return usageWriter.Show($"Unknown command: {options.Command}");
    }

    var console = serviceProvider.GetRequiredService<IConsole>();

    try
    {
        return await executor(options, CancellationToken.None).ConfigureAwait(false);
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
