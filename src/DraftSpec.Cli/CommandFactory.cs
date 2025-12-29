using DraftSpec.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli;

/// <summary>
/// Factory for creating command instances from the DI container.
/// </summary>
public class CommandFactory : ICommandFactory
{
    private readonly IServiceProvider _services;

    public CommandFactory(IServiceProvider services)
    {
        _services = services;
    }

    public ICommand? Create(string commandName)
    {
        return commandName.ToLowerInvariant() switch
        {
            "run" => _services.GetRequiredService<RunCommand>(),
            "watch" => _services.GetRequiredService<WatchCommand>(),
            "list" => _services.GetRequiredService<ListCommand>(),
            "validate" => _services.GetRequiredService<ValidateCommand>(),
            "init" => _services.GetRequiredService<InitCommand>(),
            "new" => _services.GetRequiredService<NewCommand>(),
            _ => null
        };
    }
}
