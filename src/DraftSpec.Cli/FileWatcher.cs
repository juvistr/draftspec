namespace DraftSpec.Cli;

public class FileWatcher : IFileWatcher
{
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly Action<FileChangeInfo> _onChange;
    private readonly int _debounceMs;
    private Timer? _debounceTimer;
    private readonly object _lock = new();
    private FileChangeInfo? _pendingChange;

    public FileWatcher(string path, Action<FileChangeInfo> onChange, int debounceMs = 200)
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
        if (e.Name?.StartsWith('.') == true || e.Name?.EndsWith('~') == true)
            return;

        // Normalize path for consistent comparison across multiple events
        var normalizedPath = Path.GetFullPath(e.FullPath);
        var isSpecFile = normalizedPath.EndsWith(".spec.csx", StringComparison.OrdinalIgnoreCase);

        lock (_lock)
        {
            // Track which file changed (for selective re-running)
            // If multiple files change during debounce, escalate to full run
            if (_pendingChange == null)
                _pendingChange = new FileChangeInfo(normalizedPath, isSpecFile);
            else if (_pendingChange.IsSpecFile && isSpecFile && !PathsAreEqual(_pendingChange.FilePath, normalizedPath))
                // Multiple different spec files changed - run all
                _pendingChange = new FileChangeInfo(null, false);
            else if (!isSpecFile)
                // Source file changed - run all
                _pendingChange = new FileChangeInfo(null, false);

            // Reuse timer to avoid allocations on rapid file changes
            if (_debounceTimer == null)
                // First change - create timer that fires once after debounce period
                _debounceTimer = new Timer(
                    _ => FireChange(),
                    null,
                    _debounceMs,
                    Timeout.Infinite);
            else
                // Subsequent change - reset timer to debounce again
                _debounceTimer.Change(_debounceMs, Timeout.Infinite);
        }
    }

    private void FireChange()
    {
        FileChangeInfo change;
        lock (_lock)
        {
            change = _pendingChange ?? new FileChangeInfo(null, false);
            _pendingChange = null;
        }

        _onChange(change);
    }

    /// <summary>
    /// Compares two paths for equality, using case-insensitive comparison on Windows.
    /// </summary>
    private static bool PathsAreEqual(string? path1, string? path2)
    {
        if (path1 == null || path2 == null)
            return path1 == path2;

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(path1, path2, comparison);
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        _debounceTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
