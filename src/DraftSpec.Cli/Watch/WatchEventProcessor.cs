using System.Text.RegularExpressions;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Services;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Watch;

/// <summary>
/// Processes file change events and determines the appropriate watch action.
/// </summary>
/// <remarks>
/// This is the "adapter" implementation of <see cref="IWatchEventProcessor"/>,
/// containing all the decision logic for handling file changes in watch mode.
/// </remarks>
public class WatchEventProcessor : IWatchEventProcessor
{
    private readonly ISpecChangeTracker _changeTracker;
    private readonly IStaticSpecParserFactory _parserFactory;

    /// <summary>
    /// Creates a new instance of <see cref="WatchEventProcessor"/>.
    /// </summary>
    /// <param name="changeTracker">Tracker for detecting spec-level changes.</param>
    /// <param name="parserFactory">Factory for creating spec parsers.</param>
    public WatchEventProcessor(
        ISpecChangeTracker changeTracker,
        IStaticSpecParserFactory parserFactory)
    {
        _changeTracker = changeTracker;
        _parserFactory = parserFactory;
    }

    /// <inheritdoc />
    public async Task<WatchAction> ProcessChangeAsync(
        FileChangeInfo change,
        IReadOnlyList<string> allSpecFiles,
        string basePath,
        bool incremental,
        bool noCache,
        CancellationToken ct)
    {
        // Not a spec file change or no file path - run all specs
        if (!change.IsSpecFile || change.FilePath == null)
        {
            return WatchAction.RunAll();
        }

        // Find the matching spec file from our list
        var changedSpec = FindMatchingSpecFile(allSpecFiles, change.FilePath);
        if (changedSpec == null)
        {
            // Spec file not in our tracked list - run all
            return WatchAction.RunAll();
        }

        // Non-incremental mode: run the entire file
        if (!incremental)
        {
            return WatchAction.RunFile(changedSpec);
        }

        // Incremental mode: detect spec-level changes
        return await ProcessIncrementalChangeAsync(changedSpec, basePath, noCache, ct);
    }

    private static string? FindMatchingSpecFile(IReadOnlyList<string> allSpecFiles, string filePath)
    {
        return allSpecFiles.FirstOrDefault(f =>
            string.Equals(Path.GetFullPath(f), filePath, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<WatchAction> ProcessIncrementalChangeAsync(
        string specFile,
        string basePath,
        bool noCache,
        CancellationToken ct)
    {
        // Resolve project path (directory, not file)
        var projectPath = Path.GetFullPath(basePath);
        if (File.Exists(projectPath))
        {
            projectPath = Path.GetDirectoryName(projectPath)!;
        }

        // Parse the file to get current state
        var parser = _parserFactory.Create(projectPath, useCache: !noCache);
        var newResult = await parser.ParseFileAsync(specFile, ct);

        // For now, we don't track dependencies - just spec changes
        const bool dependencyChanged = false;

        // Get changes between recorded state and new state
        var changes = _changeTracker.GetChanges(specFile, newResult, dependencyChanged);

        if (!changes.HasChanges)
        {
            return WatchAction.Skip("No spec changes detected.");
        }

        if (changes.RequiresFullRun)
        {
            var reason = changes.HasDynamicSpecs ? "dynamic specs detected" : "dependency changed";
            var message = $"Full run required: {reason}";
            return WatchAction.RunFile(specFile, message);
        }

        // Build filter pattern for changed specs
        var filterPattern = BuildFilterPattern(changes.SpecsToRun);
        var specsChangedMessage = $"Incremental: {changes.SpecsToRun.Count} spec(s) changed";

        return WatchAction.RunFiltered(specFile, filterPattern, specsChangedMessage, newResult);
    }

    /// <summary>
    /// Builds a regex pattern that matches any of the spec descriptions.
    /// </summary>
    /// <remarks>
    /// This is extracted from WatchCommand.BuildFilterPattern for reuse.
    /// </remarks>
    internal static string BuildFilterPattern(IReadOnlyList<SpecChange> specs)
    {
        if (specs.Count == 0)
            return "^$"; // Match nothing

        // Build regex that matches any of the spec descriptions
        // Use Regex.Escape() for special characters
        var escaped = specs.Select(s => Regex.Escape(s.Description));
        return $"^({string.Join("|", escaped)})$";
    }
}
