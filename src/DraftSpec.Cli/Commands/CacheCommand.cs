using DraftSpec.Cli.Options;
using DraftSpec.Scripting;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Cache management command for viewing stats and clearing cached data.
/// </summary>
public class CacheCommand : ICommand<CacheOptions>
{
    private readonly IConsole _console;

    public CacheCommand(IConsole console)
    {
        _console = console;
    }

    public Task<int> ExecuteAsync(CacheOptions options, CancellationToken ct = default)
    {
        var projectDir = Path.GetFullPath(options.Path);

        return options.Subcommand.ToLowerInvariant() switch
        {
            "stats" => ShowStats(projectDir),
            "clear" => ClearCache(projectDir),
            _ => ShowUsage(options.Subcommand)
        };
    }

    private Task<int> ShowStats(string projectDir)
    {
        _console.WriteLine("Cache Statistics:");
        _console.WriteLine($"  Location: {Path.Combine(projectDir, ".draftspec", "cache")}");
        _console.WriteLine();

        // Script compilation cache
        var scriptCache = new ScriptCompilationCache(projectDir);
        var scriptStats = scriptCache.GetStatistics();

        _console.WriteLine("  Script Compilation Cache:");
        _console.WriteLine($"    Entries: {scriptStats.EntryCount}");
        _console.WriteLine($"    Size:    {FormatSize(scriptStats.TotalSizeBytes)}");
        _console.WriteLine();

        // Parse result cache
        var parseCache = new StaticParseResultCache(projectDir);
        var parseStats = parseCache.GetStatistics();

        _console.WriteLine("  Parse Result Cache:");
        _console.WriteLine($"    Entries: {parseStats.EntryCount}");
        _console.WriteLine($"    Size:    {FormatSize(parseStats.TotalSizeBytes)}");
        _console.WriteLine();

        var totalEntries = scriptStats.EntryCount + parseStats.EntryCount;
        var totalSize = scriptStats.TotalSizeBytes + parseStats.TotalSizeBytes;

        _console.WriteLine($"  Total: {totalEntries} entries, {FormatSize(totalSize)}");

        return Task.FromResult(0);
    }

    private Task<int> ClearCache(string projectDir)
    {
        // Clear script compilation cache
        var scriptCache = new ScriptCompilationCache(projectDir);
        var scriptStats = scriptCache.GetStatistics();
        scriptCache.Clear();

        // Clear parse result cache
        var parseCache = new StaticParseResultCache(projectDir);
        var parseStats = parseCache.GetStatistics();
        parseCache.Clear();

        var totalCleared = scriptStats.EntryCount + parseStats.EntryCount;
        var totalSize = scriptStats.TotalSizeBytes + parseStats.TotalSizeBytes;

        if (totalCleared > 0)
        {
            _console.WriteSuccess($"Cleared {totalCleared} cache entries ({FormatSize(totalSize)})");
        }
        else
        {
            _console.WriteLine("Cache is already empty");
        }

        return Task.FromResult(0);
    }

    private Task<int> ShowUsage(string invalidSubcommand)
    {
        _console.WriteError($"Unknown cache subcommand: {invalidSubcommand}");
        _console.WriteLine();
        _console.WriteLine("Usage: draftspec cache <subcommand>");
        _console.WriteLine();
        _console.WriteLine("Subcommands:");
        _console.WriteLine("  stats    Show cache statistics");
        _console.WriteLine("  clear    Clear all cached data");

        return Task.FromResult(1);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes == 0) return "0 B";
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
