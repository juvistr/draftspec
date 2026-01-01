using DraftSpec.Cli;
using DraftSpec.Cli.Pipeline;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of ICommandRegistry for testing.
/// </summary>
public class MockCommandRegistry : ICommandRegistry
{
    private readonly Dictionary<string, ICommandExecutor> _executors = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _registeredCommands = [];

    /// <summary>
    /// Gets the command names that were looked up via GetExecutor.
    /// </summary>
    public List<string> GetExecutorCalls { get; } = [];

    /// <summary>
    /// Configure an executor to return for a command name.
    /// </summary>
    public MockCommandRegistry WithExecutor(string name, ICommandExecutor executor)
    {
        _executors[name] = executor;
        _registeredCommands.Add(name);
        return this;
    }

    /// <summary>
    /// Configure a command that returns a stub executor.
    /// </summary>
    public MockCommandRegistry WithCommand(string name, int returnCode = 0)
    {
        return WithExecutor(name, new StubExecutor(returnCode));
    }

    public void Register<TCommand, TOptions>(
        string name,
        Func<TCommand> commandFactory,
        Func<CliOptions, TOptions> optionsConverter)
        where TCommand : ICommand<TOptions>
    {
        _registeredCommands.Add(name);
    }

    public ICommandExecutor? GetExecutor(string name)
    {
        GetExecutorCalls.Add(name);
        return _executors.TryGetValue(name, out var executor) ? executor : null;
    }

    public IEnumerable<string> RegisteredCommands => _registeredCommands;

    private class StubExecutor : ICommandExecutor
    {
        private readonly int _returnCode;

        public StubExecutor(int returnCode) => _returnCode = returnCode;

        public Task<int> ExecuteAsync(CliOptions options, CancellationToken cancellationToken)
            => Task.FromResult(_returnCode);
    }
}
