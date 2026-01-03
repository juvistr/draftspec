using DraftSpec.Cli.CoverageMap;
using DraftSpec.Cli.Formatters;
using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli.Pipeline.Phases.CoverageMap;

/// <summary>
/// Outputs coverage map results in the requested format.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[CoverageMapResult]</c></para>
/// <para><b>Optional:</b> <c>Items[CoverageMapFormat]</c> (default Console), <c>Items[GapsOnly]</c></para>
/// <para>Terminal phase - does not call the next pipeline.</para>
/// </remarks>
public sealed class CoverageMapOutputPhase : ICommandPhase
{
    /// <summary>
    /// Exit code when command completes successfully.
    /// </summary>
    public const int ExitSuccess = 0;

    /// <summary>
    /// Exit code when gaps-only mode finds uncovered methods.
    /// </summary>
    public const int ExitGapsFound = 1;

    /// <inheritdoc />
    public Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var result = context.Get<CoverageMapResult>(ContextKeys.CoverageMapResult);
        if (result is null)
        {
            context.Console.WriteError("CoverageMapResult not set. Run CoverageMapPhase first.");
            return Task.FromResult(1);
        }

        var format = context.Get<CoverageMapFormat?>(ContextKeys.CoverageMapFormat) ?? CoverageMapFormat.Console;
        var gapsOnly = context.Get<bool>(ContextKeys.GapsOnly);

        var formatter = CreateFormatter(format);
        var output = formatter.Format(result, gapsOnly);
        context.Console.WriteLine(output);

        // Return non-zero if gaps-only mode found uncovered methods
        var exitCode = gapsOnly && result.UncoveredMethods.Count > 0 ? ExitGapsFound : ExitSuccess;
        return Task.FromResult(exitCode);
    }

    private static ICoverageMapFormatter CreateFormatter(CoverageMapFormat format)
    {
        return format switch
        {
            CoverageMapFormat.Json => new JsonCoverageMapFormatter(),
            _ => new ConsoleCoverageMapFormatter()
        };
    }
}
