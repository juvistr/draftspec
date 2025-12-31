namespace DraftSpec.Cli;

/// <summary>
/// Default factory that creates FileWatcher instances.
/// </summary>
public class FileWatcherFactory : IFileWatcherFactory
{
    public IFileWatcher Create(string path, Action<FileChangeInfo> onChange, int debounceMs = 200)
    {
        return new FileWatcher(path, onChange, debounceMs);
    }
}
