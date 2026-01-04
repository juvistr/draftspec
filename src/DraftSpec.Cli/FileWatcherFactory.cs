namespace DraftSpec.Cli;

/// <summary>
/// Default factory that creates FileWatcher instances.
/// </summary>
public class FileWatcherFactory : IFileWatcherFactory
{
    private readonly IOperatingSystem _os;

    public FileWatcherFactory(IOperatingSystem os)
    {
        _os = os;
    }

    public IFileWatcher Create(string path, int debounceMs = 200)
    {
        return new FileWatcher(path, _os, debounceMs);
    }
}
