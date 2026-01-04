using DraftSpec.Cli;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock file watcher factory for unit testing watch mode.
/// Creates and returns a pre-configured MockFileWatcher instance.
/// </summary>
public class MockFileWatcherFactory : IFileWatcherFactory
{
    private readonly MockFileWatcher _watcher;

    public bool CreateCalled { get; private set; }
    public string? LastPath { get; private set; }
    public int LastDebounceMs { get; private set; }

    public MockFileWatcherFactory() : this(new MockFileWatcher())
    {
    }

    public MockFileWatcherFactory(MockFileWatcher watcher)
    {
        _watcher = watcher;
    }

    /// <summary>
    /// Gets the mock watcher that was/will be returned by Create.
    /// Use this to trigger changes in tests.
    /// </summary>
    public MockFileWatcher Watcher => _watcher;

    public IFileWatcher Create(string path, int debounceMs = 200)
    {
        CreateCalled = true;
        LastPath = path;
        LastDebounceMs = debounceMs;
        return _watcher;
    }

    /// <summary>
    /// Convenience method to trigger a change on the watcher.
    /// </summary>
    public void TriggerChange(FileChangeInfo change)
    {
        _watcher.TriggerChange(change);
    }

    /// <summary>
    /// Convenience method to trigger a change and wait for processing.
    /// </summary>
    public Task TriggerChangeAndWaitAsync(FileChangeInfo change, TimeSpan? timeout = null)
    {
        return _watcher.TriggerChangeAndWaitAsync(change, timeout);
    }
}
