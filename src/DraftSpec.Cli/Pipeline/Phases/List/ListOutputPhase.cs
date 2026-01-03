using DraftSpec.Cli.Formatters;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Pipeline.Phases.List;

/// <summary>
/// Formats and outputs the discovered specs list.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[FilteredSpecs]</c>, <c>Items[DiscoveryErrors]</c></para>
/// <para><b>Optional:</b> <c>Items[ListFormat]</c> (defaults to Tree), <c>Items[ShowLineNumbers]</c> (defaults to true)</para>
/// <para><b>Terminal phase:</b> Does not call next pipeline delegate</para>
/// </remarks>
public class ListOutputPhase : ICommandPhase
{
    /// <inheritdoc />
    public Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var filteredSpecs = context.Get<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs);
        if (filteredSpecs == null)
        {
            context.Console.WriteError("FilteredSpecs not set. Run FilterApplyPhase first.");
            return Task.FromResult(1);
        }

        var errors = context.Get<IReadOnlyList<DiscoveryError>>(ContextKeys.DiscoveryErrors) ?? [];

        // Get formatting options with defaults
        var format = context.Get<ListFormat>(ContextKeys.ListFormat);
        var showLineNumbers = context.Items.ContainsKey(ContextKeys.ShowLineNumbers)
            ? context.Get<bool>(ContextKeys.ShowLineNumbers)
            : true;

        // Create formatter and format output
        var formatter = CreateFormatter(format, showLineNumbers);
        var output = formatter.Format(filteredSpecs, errors);

        // Write output
        context.Console.WriteLine(output);

        // Terminal phase - don't call next pipeline
        return Task.FromResult(0);
    }

    private static IListFormatter CreateFormatter(ListFormat format, bool showLineNumbers)
    {
        return format switch
        {
            ListFormat.Tree => new TreeListFormatter(showLineNumbers),
            ListFormat.Flat => new FlatListFormatter(showLineNumbers),
            ListFormat.Json => new JsonListFormatter(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown list format")
        };
    }
}
