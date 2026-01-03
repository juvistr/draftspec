using DraftSpec.Scripting;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Pipeline.Phases.Cache;

/// <summary>
/// Executes cache operations (stats or clear) based on subcommand.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ProjectPath]</c> - directory containing cache</para>
/// <para><b>Reads:</b> <c>Items[CacheSubcommand]</c> - operation to perform (stats, clear)</para>
/// <para><b>Short-circuits:</b> On unknown subcommand (returns 1)</para>
/// </remarks>
public class CacheOperationPhase : ICommandPhase
{
    /// <inheritdoc />
    public Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var projectPath = context.Get<string>(ContextKeys.ProjectPath);

        if (string.IsNullOrEmpty(projectPath))
        {
            context.Console.WriteError("ProjectPath not set");
            return Task.FromResult(1);
        }

        var subcommand = context.Get<string>(ContextKeys.CacheSubcommand) ?? "";

        var result = subcommand.ToLowerInvariant() switch
        {
            "stats" => ShowStats(context.Console, projectPath),
            "clear" => ClearCache(context.Console, projectPath),
            _ => ShowUsage(context.Console, subcommand)
        };

        if (result != 0)
            return Task.FromResult(result);

        return pipeline(context, ct);
    }

    private static int ShowStats(IConsole console, string projectDir)
    {
        console.WriteLine("Cache Statistics:");
        console.WriteLine($"  Location: {Path.Combine(projectDir, ".draftspec", "cache")}");
        console.WriteLine();

        // Script compilation cache
        var scriptCache = new ScriptCompilationCache(projectDir);
        var scriptStats = scriptCache.GetStatistics();

        console.WriteLine("  Script Compilation Cache:");
        console.WriteLine($"    Entries: {scriptStats.EntryCount}");
        console.WriteLine($"    Size:    {FormatSize(scriptStats.TotalSizeBytes)}");
        console.WriteLine();

        // Parse result cache
        var parseCache = new StaticParseResultCache(projectDir);
        var parseStats = parseCache.GetStatistics();

        console.WriteLine("  Parse Result Cache:");
        console.WriteLine($"    Entries: {parseStats.EntryCount}");
        console.WriteLine($"    Size:    {FormatSize(parseStats.TotalSizeBytes)}");
        console.WriteLine();

        var totalEntries = scriptStats.EntryCount + parseStats.EntryCount;
        var totalSize = scriptStats.TotalSizeBytes + parseStats.TotalSizeBytes;

        console.WriteLine($"  Total: {totalEntries} entries, {FormatSize(totalSize)}");

        return 0;
    }

    private static int ClearCache(IConsole console, string projectDir)
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
            console.WriteSuccess($"Cleared {totalCleared} cache entries ({FormatSize(totalSize)})");
        }
        else
        {
            console.WriteLine("Cache is already empty");
        }

        return 0;
    }

    private static int ShowUsage(IConsole console, string invalidSubcommand)
    {
        console.WriteError($"Unknown cache subcommand: {invalidSubcommand}");
        console.WriteLine();
        console.WriteLine("Usage: draftspec cache <subcommand>");
        console.WriteLine();
        console.WriteLine("Subcommands:");
        console.WriteLine("  stats    Show cache statistics");
        console.WriteLine("  clear    Clear all cached data");

        return 1;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes == 0) return "0 B";
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
