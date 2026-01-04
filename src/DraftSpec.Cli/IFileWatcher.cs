using System.Runtime.CompilerServices;

namespace DraftSpec.Cli;

/// <summary>
/// Watches for file changes and provides async enumerable of changes.
/// </summary>
public interface IFileWatcher : IDisposable
{
    /// <summary>
    /// Asynchronously enumerates file changes as they occur.
    /// </summary>
    /// <param name="ct">Cancellation token to stop watching</param>
    /// <returns>Async enumerable of file change information</returns>
    IAsyncEnumerable<FileChangeInfo> WatchAsync(CancellationToken ct = default);
}
