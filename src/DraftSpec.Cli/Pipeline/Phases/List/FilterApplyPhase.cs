using DraftSpec.Cli.Options;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Pipeline.Phases.List;

/// <summary>
/// Converts parsed specs to discovered specs and applies filter options.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ProjectPath]</c>, <c>Items[ParsedSpecs]</c></para>
/// <para><b>Optional:</b> <c>Items[Filter]</c>, <c>Items[FocusedOnly]</c>, <c>Items[PendingOnly]</c>, <c>Items[SkippedOnly]</c>, <c>Items[ContextFilter]</c></para>
/// <para><b>Produces:</b> <c>Items[FilteredSpecs]</c>, <c>Items[DiscoveryErrors]</c></para>
/// </remarks>
public class FilterApplyPhase : ICommandPhase
{
    /// <inheritdoc />
    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var projectPath = context.Get<string>(ContextKeys.ProjectPath);
        if (string.IsNullOrEmpty(projectPath))
        {
            context.Console.WriteError("ProjectPath not set. Run PathResolutionPhase first.");
            return 1;
        }

        var parsedSpecs = context.Get<IReadOnlyDictionary<string, StaticParseResult>>(ContextKeys.ParsedSpecs);
        if (parsedSpecs == null)
        {
            context.Console.WriteError("ParsedSpecs not set. Run SpecParsingPhase first.");
            return 1;
        }

        // Convert StaticSpecs to DiscoveredSpecs
        var allSpecs = new List<DiscoveredSpec>();
        var allErrors = new List<DiscoveryError>();

        foreach (var (specFile, result) in parsedSpecs)
        {
            var relativePath = Path.GetRelativePath(projectPath, specFile);

            foreach (var staticSpec in result.Specs)
            {
                var id = GenerateId(relativePath, staticSpec.ContextPath, staticSpec.Description);
                var displayName = GenerateDisplayName(staticSpec.ContextPath, staticSpec.Description);

                allSpecs.Add(new DiscoveredSpec
                {
                    Id = id,
                    Description = staticSpec.Description,
                    DisplayName = displayName,
                    ContextPath = staticSpec.ContextPath,
                    SourceFile = specFile,
                    RelativeSourceFile = relativePath,
                    LineNumber = staticSpec.LineNumber,
                    IsPending = staticSpec.IsPending,
                    IsSkipped = staticSpec.Type == StaticSpecType.Skipped,
                    IsFocused = staticSpec.Type == StaticSpecType.Focused,
                    Tags = []
                });
            }

            // Report warnings as errors for files that couldn't be fully parsed
            if (!result.IsComplete)
            {
                foreach (var warning in result.Warnings)
                {
                    allErrors.Add(new DiscoveryError
                    {
                        SourceFile = specFile,
                        RelativeSourceFile = relativePath,
                        Message = warning
                    });
                }
            }
        }

        // Get filter options from context
        var focusedOnly = context.Get<bool>(ContextKeys.FocusedOnly);
        var pendingOnly = context.Get<bool>(ContextKeys.PendingOnly);
        var skippedOnly = context.Get<bool>(ContextKeys.SkippedOnly);
        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        var contextFilter = context.Get<string>(ContextKeys.ContextFilter);

        // Apply filters
        var filteredSpecs = ApplyFilters(allSpecs, focusedOnly, pendingOnly, skippedOnly, filter, contextFilter);

        // Store results in context
        context.Set<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs, filteredSpecs);
        context.Set<IReadOnlyList<DiscoveryError>>(ContextKeys.DiscoveryErrors, allErrors);

        return await pipeline(context, ct);
    }

    private static string GenerateId(string relativePath, IReadOnlyList<string> contextPath, string description)
    {
        var path = string.Join("/", contextPath);
        return $"{relativePath}:{path}/{description}";
    }

    private static string GenerateDisplayName(IReadOnlyList<string> contextPath, string description)
    {
        if (contextPath.Count == 0)
            return description;

        return string.Join(" > ", contextPath) + " > " + description;
    }

    private static IReadOnlyList<DiscoveredSpec> ApplyFilters(
        IReadOnlyList<DiscoveredSpec> specs,
        bool focusedOnly,
        bool pendingOnly,
        bool skippedOnly,
        FilterOptions? filter,
        string? contextFilter)
    {
        var filtered = specs.AsEnumerable();

        // Status filters (OR'd together if multiple specified)
        var hasStatusFilter = focusedOnly || pendingOnly || skippedOnly;
        if (hasStatusFilter)
        {
            filtered = filtered.Where(s =>
                (focusedOnly && s.IsFocused) ||
                (pendingOnly && s.IsPending) ||
                (skippedOnly && s.IsSkipped));
        }

        // Context filter - matches context path elements or display name
        if (!string.IsNullOrEmpty(contextFilter))
        {
            var matcher = PatternMatcher.Create(contextFilter);
            filtered = filtered.Where(s =>
                s.ContextPath.Any(c => matcher.Matches(c)) ||
                matcher.Matches(s.DisplayName));
        }

        // Pattern filter on name (AND'd with status filters)
        if (!string.IsNullOrEmpty(filter?.FilterName))
        {
            var matcher = PatternMatcher.Create(filter.FilterName);
            filtered = filtered.Where(s => matcher.Matches(s.DisplayName));
        }

        // Tag filter (AND'd with other filters)
        if (!string.IsNullOrEmpty(filter?.FilterTags))
        {
            var tags = filter.FilterTags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            filtered = filtered.Where(s => s.Tags.Any(t => tags.Contains(t)));
        }

        return filtered.ToList();
    }
}
