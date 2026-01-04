using DraftSpec.Cli.Formatters;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Pipeline.Phases.Docs;

/// <summary>
/// Formats discovered specs as documentation and outputs them.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[FilteredSpecs]</c>, <c>Items[ProjectPath]</c></para>
/// <para><b>Optional:</b> <c>Items[DocsFormat]</c>, <c>Items[WithResults]</c>, <c>Items[ResultsFile]</c></para>
/// <para><b>Terminal phase:</b> Does not call next pipeline.</para>
/// </remarks>
public class DocsOutputPhase : ICommandPhase
{
    /// <inheritdoc />
    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var filteredSpecs = context.Get<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs);
        if (filteredSpecs == null)
        {
            context.Console.WriteError("FilteredSpecs not set. Run FilterApplyPhase first.");
            return 1;
        }

        var projectPath = context.Get<string>(ContextKeys.ProjectPath);
        if (string.IsNullOrEmpty(projectPath))
        {
            context.Console.WriteError("ProjectPath not set. Run PathResolutionPhase first.");
            return 1;
        }

        // Get options from context
        var format = context.Items.ContainsKey(ContextKeys.DocsFormat)
            ? context.Get<DocsFormat>(ContextKeys.DocsFormat)
            : DocsFormat.Markdown;
        var withResults = context.Get<bool>(ContextKeys.WithResults);
        var resultsFile = context.Get<string>(ContextKeys.ResultsFile);

        // Load results if requested
        IReadOnlyDictionary<string, string>? results = null;
        if (withResults)
        {
            results = await LoadResultsAsync(context, resultsFile, ct);
        }

        // Format output
        var formatter = CreateFormatter(format);
        var metadata = new DocsMetadata(
            DateTime.UtcNow,
            Path.GetRelativePath(Environment.CurrentDirectory, projectPath),
            results);
        var output = formatter.Format(filteredSpecs, metadata);

        // Write output
        context.Console.WriteLine(output);

        return 0;
    }

    private async Task<IReadOnlyDictionary<string, string>?> LoadResultsAsync(
        CommandContext context,
        string? resultsFile,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(resultsFile))
        {
            context.Console.WriteError("--with-results requires --results-file to specify the JSON results file.");
            return null;
        }

        if (!context.FileSystem.FileExists(resultsFile))
        {
            context.Console.WriteError($"Results file not found: {resultsFile}");
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(resultsFile, ct).ConfigureAwait(false);
            var report = SpecReport.FromJson(json);

            // Flatten results to dictionary of ID -> status
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            FlattenResults(report.Contexts, [], results);
            return results;
        }
        catch (Exception ex)
        {
            context.Console.WriteError($"Failed to parse results file: {ex.Message}");
            return null;
        }
    }

    private static void FlattenResults(
        IList<SpecContextReport> contexts,
        List<string> path,
        Dictionary<string, string> results)
    {
        foreach (var context in contexts)
        {
            path.Add(context.Description);

            foreach (var spec in context.Specs)
            {
                // Generate ID matching the format used in discovery
                var contextPath = string.Join("/", path);
                var id = $":{contextPath}/{spec.Description}";
                results[id] = spec.Status;
            }

            FlattenResults(context.Contexts, path, results);
            path.RemoveAt(path.Count - 1);
        }
    }

    private static IDocsFormatter CreateFormatter(DocsFormat format)
    {
        return format switch
        {
            DocsFormat.Markdown => new MarkdownDocsFormatter(),
            DocsFormat.Html => new HtmlDocsFormatter(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown docs format")
        };
    }
}
