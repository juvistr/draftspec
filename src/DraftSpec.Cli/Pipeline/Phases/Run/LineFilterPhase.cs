using System.Text.RegularExpressions;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Services;

namespace DraftSpec.Cli.Pipeline.Phases.Run;

/// <summary>
/// Parses spec files and builds filter patterns for line number selections.
/// Converts line:number syntax into FilterName patterns.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ProjectPath]</c></para>
/// <para><b>Optional:</b> <c>Items[LineFilters]</c>, <c>Items[NoCache]</c>, <c>Items[Filter]</c></para>
/// <para><b>Modifies:</b> <c>Items[Filter]</c> (adds to FilterName)</para>
/// <para><b>Short-circuits:</b> Returns 1 if line filters specified but no specs found</para>
/// </remarks>
public class LineFilterPhase : ICommandPhase
{
    private readonly IStaticSpecParserFactory _parserFactory;

    public LineFilterPhase(IStaticSpecParserFactory parserFactory)
    {
        _parserFactory = parserFactory;
    }

    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var lineFilters = context.Get<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters);
        if (lineFilters is not { Count: > 0 })
            return await pipeline(context, ct);

        var projectPath = context.Get<string>(ContextKeys.ProjectPath);
        if (string.IsNullOrEmpty(projectPath))
        {
            context.Console.WriteError("ProjectPath not set. Run PathResolutionPhase first.");
            return 1;
        }

        var noCache = context.Get<bool>(ContextKeys.NoCache);
        var parser = _parserFactory.Create(projectPath, useCache: !noCache);

        var matchingDisplayNames = new List<string>();

        foreach (var filter in lineFilters)
        {
            var filePath = Path.GetFullPath(filter.File, projectPath);
            if (!context.FileSystem.FileExists(filePath))
            {
                context.Console.WriteWarning($"File not found: {filter.File}");
                continue;
            }

            var result = await parser.ParseFileAsync(filePath, ct);

            // Find specs at the specified line numbers
            foreach (var lineNumber in filter.Lines)
            {
                // Check if any spec is at this line
                var matchingSpecs = result.Specs
                    .Where(s => s.LineNumber == lineNumber)
                    .ToList();

                if (matchingSpecs.Count > 0)
                {
                    foreach (var spec in matchingSpecs)
                    {
                        var displayName = GenerateDisplayName(spec.ContextPath, spec.Description);
                        matchingDisplayNames.Add(displayName);
                    }
                }
                else
                {
                    // No spec at exact line - check nearby lines (within 1 line)
                    var nearbySpecs = result.Specs
                        .Where(s => Math.Abs(s.LineNumber - lineNumber) <= 1)
                        .ToList();

                    foreach (var spec in nearbySpecs)
                    {
                        var displayName = GenerateDisplayName(spec.ContextPath, spec.Description);
                        matchingDisplayNames.Add(displayName);
                    }
                }
            }
        }

        if (matchingDisplayNames.Count == 0)
        {
            context.Console.WriteError("No specs found at the specified line numbers.");
            return 1;
        }

        // Build filter pattern
        var escapedNames = matchingDisplayNames.Distinct().Select(Regex.Escape);
        var filterPattern = $"^({string.Join("|", escapedNames)})$";

        // Merge with existing filter
        var existingFilter = context.Get<FilterOptions>(ContextKeys.Filter) ?? new FilterOptions();
        existingFilter.FilterName = string.IsNullOrEmpty(existingFilter.FilterName)
            ? filterPattern
            : $"({existingFilter.FilterName})|({filterPattern})";
        context.Set(ContextKeys.Filter, existingFilter);

        return await pipeline(context, ct);
    }

    private static string GenerateDisplayName(IReadOnlyList<string> contextPath, string description)
    {
        if (contextPath.Count == 0)
            return description;

        return string.Join(" > ", contextPath) + " > " + description;
    }
}
