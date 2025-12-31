using DraftSpec.Cli;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock file watcher factory for unit testing watch mode.
/// Allows tests to trigger file change events programmatically.
/// </summary>
public class MockFileWatcherFactory : IFileWatcherFactory
{
    private Action<FileChangeInfo>? _onChange;

    public bool CreateCalled { get; private set; }
    public string? LastPath { get; private set; }
    public int LastDebounceMs { get; private set; }
    public bool OnChangeCallbackInvoked { get; private set; }

    public IFileWatcher Create(string path, Action<FileChangeInfo> onChange, int debounceMs = 200)
    {
        CreateCalled = true;
        LastPath = path;
        LastDebounceMs = debounceMs;
        _onChange = onChange;
        return new MockFileWatcher();
    }

    /// <summary>
    /// Triggers a file change event for testing.
    /// </summary>
    public void TriggerChange(FileChangeInfo change)
    {
        OnChangeCallbackInvoked = true;
        _onChange?.Invoke(change);
    }
}
