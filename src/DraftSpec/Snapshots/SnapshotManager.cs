using DraftSpec.Providers;

namespace DraftSpec.Snapshots;

/// <summary>
/// Manages snapshot storage, retrieval, and comparison.
/// Snapshots are stored in __snapshots__ directory alongside spec files.
/// </summary>
public class SnapshotManager
{
    private const string SnapshotDirName = "__snapshots__";

    /// <summary>
    /// Environment variable to enable snapshot update mode.
    /// Set to "true" or "1" to create/update snapshots.
    /// </summary>
    public const string UpdateEnvVar = "DRAFTSPEC_UPDATE_SNAPSHOTS";

    private readonly IEnvironmentProvider _env;
    private readonly Dictionary<string, Dictionary<string, string>> _snapshotCache = new();
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new snapshot manager.
    /// </summary>
    /// <param name="env">Optional environment provider for testing. Defaults to system environment.</param>
    public SnapshotManager(IEnvironmentProvider? env = null)
    {
        _env = env ?? SystemEnvironmentProvider.Instance;
    }

    /// <summary>
    /// Whether update mode is enabled via environment variable.
    /// </summary>
    public bool UpdateMode =>
        _env.GetEnvironmentVariable(UpdateEnvVar) is "true" or "1";

    /// <summary>
    /// Get the snapshot directory path for a spec file.
    /// </summary>
    public static string GetSnapshotDir(string specFilePath)
    {
        var dir = Path.GetDirectoryName(specFilePath) ?? ".";
        return Path.Combine(dir, SnapshotDirName);
    }

    /// <summary>
    /// Get the snapshot file path for a spec file.
    /// </summary>
    public static string GetSnapshotFilePath(string specFilePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(specFilePath);
        return Path.Combine(GetSnapshotDir(specFilePath), $"{fileName}.snap.json");
    }

    /// <summary>
    /// Compare an actual value against the stored snapshot.
    /// </summary>
    /// <typeparam name="T">The type of value to snapshot</typeparam>
    /// <param name="actual">The actual value</param>
    /// <param name="specFilePath">Path to the spec file</param>
    /// <param name="snapshotKey">Key for this snapshot (usually FullDescription)</param>
    /// <param name="customName">Optional custom snapshot name</param>
    /// <returns>Result of the comparison</returns>
    public SnapshotResult Match<T>(
        T actual,
        string specFilePath,
        string snapshotKey,
        string? customName = null)
    {
        var key = SanitizeKey(customName ?? snapshotKey);
        var actualJson = SnapshotSerializer.Serialize(actual);
        var snapshotPath = GetSnapshotFilePath(specFilePath);

        lock (_lock)
        {
            var snapshots = LoadSnapshots(snapshotPath);

            if (!snapshots.TryGetValue(key, out var expectedJson))
            {
                // Snapshot doesn't exist - create it on first run or in update mode
                // Note: New snapshots are always created, regardless of whether the file exists
                // The "Missing" status is only used when explicitly disabled (future feature)
                snapshots[key] = actualJson;
                SaveSnapshots(snapshotPath, snapshots);
                return SnapshotResult.Created(key);
            }

            if (UpdateMode)
            {
                // Update existing snapshot
                snapshots[key] = actualJson;
                SaveSnapshots(snapshotPath, snapshots);
                return SnapshotResult.Updated(key);
            }

            // Compare
            if (NormalizeJson(actualJson) == NormalizeJson(expectedJson))
                return SnapshotResult.Matched(key);

            var diff = SnapshotDiff.Generate(expectedJson, actualJson);
            return SnapshotResult.Mismatched(key, expectedJson, actualJson, diff);
        }
    }

    /// <summary>
    /// Clear the in-memory snapshot cache.
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            _snapshotCache.Clear();
        }
    }

    private Dictionary<string, string> LoadSnapshots(string path)
    {
        if (_snapshotCache.TryGetValue(path, out var cached))
            return cached;

        if (!File.Exists(path))
            return _snapshotCache[path] = new Dictionary<string, string>();

        try
        {
            var json = File.ReadAllText(path);
            var snapshots = SnapshotSerializer.DeserializeSnapshots(json)
                ?? new Dictionary<string, string>();
            return _snapshotCache[path] = snapshots;
        }
        catch (Exception)
        {
            // If we can't read the file, start fresh
            return _snapshotCache[path] = new Dictionary<string, string>();
        }
    }

    private void SaveSnapshots(string path, Dictionary<string, string> snapshots)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = SnapshotSerializer.SerializeSnapshots(snapshots);
        File.WriteAllText(path, json);
        _snapshotCache[path] = snapshots;
    }

    private static string SanitizeKey(string key)
    {
        // Replace characters that might cause issues in JSON keys
        return key
            .Replace("\\", "/")
            .Replace("\"", "'");
    }

    private static string NormalizeJson(string json)
    {
        // Normalize line endings for comparison
        return json.Replace("\r\n", "\n").TrimEnd();
    }
}
