using DraftSpec.Cli.Services;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Pipeline.Phases.Common;

/// <summary>
/// Parses spec files using static analysis, setting <see cref="ContextKeys.ParsedSpecs"/>.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ProjectPath]</c> and <c>Items[SpecFiles]</c> are set</para>
/// <para><b>Produces:</b> <c>Items[ParsedSpecs]</c> - dictionary of file path to parse result</para>
/// <para><b>Short-circuits:</b> If parse errors occur (returns 1)</para>
/// </remarks>
public class SpecParsingPhase : ICommandPhase
{
    private readonly IStaticSpecParserFactory _parserFactory;

    /// <summary>
    /// Create a new spec parsing phase.
    /// </summary>
    /// <param name="parserFactory">Factory for creating spec parsers.</param>
    public SpecParsingPhase(IStaticSpecParserFactory parserFactory)
    {
        _parserFactory = parserFactory;
    }

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

        var specFiles = context.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
        if (specFiles == null || specFiles.Count == 0)
        {
            context.Console.WriteError("SpecFiles not set. Run SpecDiscoveryPhase first.");
            return 1;
        }

        IStaticSpecParser parser;
        try
        {
            parser = _parserFactory.Create(projectPath, useCache: true);
        }
        catch (Exception ex)
        {
            context.Console.WriteError($"Failed to create parser: {ex.Message}");
            return 1;
        }

        var results = new Dictionary<string, StaticParseResult>();
        var hasErrors = false;

        foreach (var specFile in specFiles)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var result = await parser.ParseFileAsync(specFile, ct);
                results[specFile] = result;

                // Report warnings
                foreach (var warning in result.Warnings)
                {
                    var relativePath = Path.GetRelativePath(projectPath, specFile);
                    context.Console.WriteWarning($"{relativePath}: {warning}");
                }
            }
            catch (Exception ex)
            {
                var relativePath = Path.GetRelativePath(projectPath, specFile);
                context.Console.WriteError($"Failed to parse {relativePath}: {ex.Message}");
                hasErrors = true;
            }
        }

        if (hasErrors)
        {
            return 1;
        }

        context.Set<IReadOnlyDictionary<string, StaticParseResult>>(
            ContextKeys.ParsedSpecs,
            results);

        return await pipeline(context, ct);
    }
}
