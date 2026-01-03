using DraftSpec.Cli.Services;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Pipeline.Phases.Validate;

/// <summary>
/// Validates parsed specs and categorizes issues as errors or warnings.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ProjectPath]</c>, <c>Items[SpecFiles]</c></para>
/// <para><b>Produces:</b> <c>Items[ValidationResults]</c></para>
/// </remarks>
public class ValidationPhase : ICommandPhase
{
    private readonly IStaticSpecParserFactory _parserFactory;

    public ValidationPhase(IStaticSpecParserFactory parserFactory)
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

        // Parse and validate each file
        var parser = _parserFactory.Create(projectPath, useCache: true);
        var results = new List<FileValidationResult>();

        foreach (var specFile in specFiles)
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(projectPath, specFile);
            var fileResult = new FileValidationResult { FilePath = relativePath };

            try
            {
                var parseResult = await parser.ParseFileAsync(specFile, ct);
                fileResult.SpecCount = parseResult.Specs.Count;

                // Categorize issues from warnings
                foreach (var warning in parseResult.Warnings)
                {
                    var issue = ParseIssue(warning);

                    if (IsError(issue))
                    {
                        fileResult.Errors.Add(issue);
                    }
                    else
                    {
                        fileResult.Warnings.Add(issue);
                    }
                }
            }
            catch (Exception ex)
            {
                fileResult.Errors.Add(new ValidationIssue
                {
                    Message = $"Parse error: {ex.Message}",
                    LineNumber = null
                });
            }

            results.Add(fileResult);
        }

        context.Set<IReadOnlyList<FileValidationResult>>(ContextKeys.ValidationResults, results);

        return await pipeline(context, ct);
    }

    private static ValidationIssue ParseIssue(string warning)
    {
        // Parse warnings like "Line 15: 'describe' has dynamic description - cannot analyze statically"
        var issue = new ValidationIssue { Message = warning };

        if (warning.StartsWith("Line ", StringComparison.OrdinalIgnoreCase))
        {
            var colonIndex = warning.IndexOf(':');
            if (colonIndex > 5)
            {
                var lineStr = warning[5..colonIndex];
                if (int.TryParse(lineStr, out var lineNumber))
                {
                    issue.LineNumber = lineNumber;
                    issue.Message = warning[(colonIndex + 1)..].Trim();
                }
            }
        }

        return issue;
    }

    private static bool IsError(ValidationIssue issue)
    {
        var msg = issue.Message.ToLowerInvariant();

        // These are errors (always fail)
        if (msg.Contains("missing description argument", StringComparison.Ordinal))
            return true;
        if (msg.Contains("empty description", StringComparison.Ordinal))
            return true;
        if (msg.Contains("parse error", StringComparison.Ordinal))
            return true;
        if (msg.Contains("syntax error", StringComparison.Ordinal))
            return true;

        // Everything else is a warning
        return false;
    }
}
