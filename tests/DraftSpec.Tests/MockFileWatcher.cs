using DraftSpec.Cli;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock file watcher that does nothing.
/// Used by MockFileWatcherFactory.
/// </summary>
public class MockFileWatcher : IFileWatcher
{
    public void Dispose() { }
}
