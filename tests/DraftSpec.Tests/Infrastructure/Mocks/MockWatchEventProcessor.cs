using DraftSpec.Cli;
using DraftSpec.Cli.Watch;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Configurable mock for IWatchEventProcessor.
/// By default, delegates to a real WatchEventProcessor, but can be configured
/// to return specific actions.
/// </summary>
public class MockWatchEventProcessor : IWatchEventProcessor
{
    private readonly IWatchEventProcessor? _delegate;
    private WatchAction? _nextAction;

    /// <summary>
    /// Tracks calls to ProcessChangeAsync.
    /// </summary>
    public List<(FileChangeInfo Change, IReadOnlyList<string> AllSpecFiles, string BasePath, bool Incremental, bool NoCache)> Calls { get; } = [];

    /// <summary>
    /// Create a mock that always returns RunAll.
    /// </summary>
    public MockWatchEventProcessor()
    {
    }

    /// <summary>
    /// Create a mock that delegates to the specified processor.
    /// </summary>
    public MockWatchEventProcessor(IWatchEventProcessor delegateProcessor)
    {
        _delegate = delegateProcessor;
    }

    /// <summary>
    /// Configure the next action to return.
    /// </summary>
    public MockWatchEventProcessor WithNextAction(WatchAction action)
    {
        _nextAction = action;
        return this;
    }

    /// <inheritdoc />
    public async Task<WatchAction> ProcessChangeAsync(
        FileChangeInfo change,
        IReadOnlyList<string> allSpecFiles,
        string basePath,
        bool incremental,
        bool noCache,
        CancellationToken ct)
    {
        Calls.Add((change, allSpecFiles, basePath, incremental, noCache));

        if (_nextAction != null)
        {
            return _nextAction;
        }

        if (_delegate != null)
        {
            return await _delegate.ProcessChangeAsync(change, allSpecFiles, basePath, incremental, noCache, ct);
        }

        return WatchAction.RunAll();
    }
}
