using DraftSpec.Cli.Interactive;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Services;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Pipeline.Phases.Run;

/// <summary>
/// Prompts the user to interactively select specs to run.
/// Parses spec files to build a selection list, then adds a filter.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[Interactive]</c> == true, <c>Items[SpecFiles]</c>, <c>Items[ProjectPath]</c></para>
/// <para><b>Optional:</b> <c>Items[NoCache]</c>, <c>Items[Filter]</c></para>
/// <para><b>Modifies:</b> <c>Items[Filter]</c> (adds FilterName pattern from selected specs)</para>
/// <para><b>Short-circuits:</b> Returns 0 if user cancels or no specs found</para>
/// </remarks>
public class InteractiveSelectionPhase : ICommandPhase
{
    private readonly ISpecSelector _selector;
    private readonly IStaticSpecParserFactory _parserFactory;

    public InteractiveSelectionPhase(
        ISpecSelector selector,
        IStaticSpecParserFactory parserFactory)
    {
        _selector = selector;
        _parserFactory = parserFactory;
    }

    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var interactive = context.Get<bool>(ContextKeys.Interactive);
        if (!interactive)
            return await pipeline(context, ct);

        var projectPath = context.Get<string>(ContextKeys.ProjectPath);
        if (string.IsNullOrEmpty(projectPath))
        {
            context.Console.WriteError("ProjectPath not set. Run PathResolutionPhase first.");
            return 1;
        }

        var specFiles = context.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
        if (specFiles == null || specFiles.Count == 0)
        {
            context.Console.WriteLine("No spec files for interactive selection.");
            return 0;
        }

        var noCache = context.Get<bool>(ContextKeys.NoCache);

        // Parse all specs to get metadata for selection UI
        var parser = _parserFactory.Create(projectPath, useCache: !noCache);
        var discoveredSpecs = new List<DiscoveredSpec>();

        foreach (var specFile in specFiles)
        {
            var result = await parser.ParseFileAsync(specFile, ct);
            var relativePath = Path.GetRelativePath(projectPath, specFile);
            foreach (var spec in result.Specs)
            {
                discoveredSpecs.Add(new DiscoveredSpec
                {
                    Id = GenerateSpecId(specFile, spec.ContextPath, spec.Description, projectPath),
                    Description = spec.Description,
                    DisplayName = GenerateDisplayName(spec.ContextPath, spec.Description),
                    ContextPath = spec.ContextPath,
                    SourceFile = specFile,
                    RelativeSourceFile = relativePath,
                    LineNumber = spec.LineNumber,
                    IsPending = spec.IsPending,
                    IsSkipped = spec.Type == StaticSpecType.Skipped,
                    IsFocused = spec.Type == StaticSpecType.Focused
                });
            }
        }

        if (discoveredSpecs.Count == 0)
        {
            context.Console.WriteLine("No specs found for interactive selection.");
            return 0;
        }

        // Show selection UI
        var selectionResult = await _selector.SelectAsync(discoveredSpecs, ct);
        if (selectionResult.Cancelled)
        {
            context.Console.WriteLine("Selection cancelled.");
            return 0;
        }

        if (selectionResult.SelectedDisplayNames.Count == 0)
        {
            context.Console.WriteLine("No specs selected.");
            return 0;
        }

        // Build filter pattern from selected display names
        var filterPattern = BuildFilterPattern(selectionResult.SelectedDisplayNames);
        MergeFilter(context, filterPattern);

        return await pipeline(context, ct);
    }

    /// <summary>
    /// Generates a stable spec ID from file path and context path.
    /// Format: "relative/path.spec.csx:Context1/Context2/SpecDescription"
    /// </summary>
    private static string GenerateSpecId(
        string specFile,
        IReadOnlyList<string> contextPath,
        string description,
        string projectPath)
    {
        var relativePath = Path.GetRelativePath(projectPath, specFile);
        var pathPart = string.Join("/", contextPath);
        return $"{relativePath}:{pathPart}/{description}";
    }

    /// <summary>
    /// Generates a human-readable display name.
    /// Format: "Context1 > Context2 > SpecDescription"
    /// </summary>
    private static string GenerateDisplayName(
        IReadOnlyList<string> contextPath,
        string description)
    {
        if (contextPath.Count == 0)
            return description;
        return string.Join(" > ", contextPath) + " > " + description;
    }

    /// <summary>
    /// Builds a regex filter pattern that matches any of the selected display names.
    /// Escapes special regex characters and joins with OR.
    /// </summary>
    private static string BuildFilterPattern(IReadOnlyList<string> displayNames)
    {
        var escapedNames = displayNames
            .Select(EscapeRegex)
            .ToList();

        // Use exact match with anchors
        return "^(" + string.Join("|", escapedNames) + ")$";
    }

    private static string EscapeRegex(string input)
    {
        // Escape regex special characters
        return System.Text.RegularExpressions.Regex.Escape(input);
    }

    /// <summary>
    /// Merges the filter pattern into the existing filter options.
    /// </summary>
    private static void MergeFilter(CommandContext context, string filterPattern)
    {
        var existing = context.Get<FilterOptions>(ContextKeys.Filter);
        if (existing == null)
        {
            context.Set(ContextKeys.Filter, new FilterOptions { FilterName = filterPattern });
        }
        else
        {
            // Combine with existing FilterName using AND logic (both must match)
            existing.FilterName = string.IsNullOrEmpty(existing.FilterName)
                ? filterPattern
                : $"(?={existing.FilterName})(?={filterPattern})";
        }
    }
}
