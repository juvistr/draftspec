namespace DraftSpec.Cli;

/// <summary>
/// Factory for creating file watchers.
/// </summary>
public interface IFileWatcherFactory
{
    /// <summary>
    /// Create a file watcher for the specified path.
    /// </summary>
    /// <param name="path">Path to watch (file or directory)</param>
    /// <param name="onChange">Callback invoked when files change</param>
    /// <param name="debounceMs">Debounce delay in milliseconds</param>
    IFileWatcher Create(string path, Action<FileChangeInfo> onChange, int debounceMs = 200);
}
