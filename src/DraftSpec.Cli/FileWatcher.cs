namespace DraftSpec.Cli;

public class FileWatcher : IDisposable
{
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly Action _onChange;
    private readonly int _debounceMs;
    private CancellationTokenSource? _debounceCts;
    private readonly object _lock = new();

    public FileWatcher(string path, Action onChange, int debounceMs = 200)
    {
        _onChange = onChange;
        _debounceMs = debounceMs;

        var fullPath = Path.GetFullPath(path);
        var watchPath = Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath)!;

        // Watch .spec.csx files
        var csxWatcher = CreateWatcher(watchPath, "*.spec.csx");
        _watchers.Add(csxWatcher);

        // Watch .cs files (source changes)
        var csWatcher = CreateWatcher(watchPath, "*.cs");
        _watchers.Add(csWatcher);
    }

    private FileSystemWatcher CreateWatcher(string path, string filter)
    {
        var watcher = new FileSystemWatcher(path, filter)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Renamed += OnFileChanged;

        return watcher;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Skip temporary files
        if (e.Name?.StartsWith(".") == true || e.Name?.EndsWith("~") == true)
            return;

        lock (_lock)
        {
            // Cancel any pending debounce
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();

            var token = _debounceCts.Token;
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_debounceMs, token);
                    if (!token.IsCancellationRequested)
                    {
                        _onChange();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Debounce was cancelled by new change
                }
            });
        }
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _debounceCts?.Dispose();
    }
}
