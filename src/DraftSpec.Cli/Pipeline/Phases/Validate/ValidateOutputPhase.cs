namespace DraftSpec.Cli.Pipeline.Phases.Validate;

/// <summary>
/// Outputs validation results and returns appropriate exit code.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ValidationResults]</c></para>
/// <para><b>Optional:</b> <c>Items[Quiet]</c>, <c>Items[Strict]</c></para>
/// <para><b>Terminal phase:</b> Does not call next pipeline.</para>
/// <para><b>Exit codes:</b> 0 = success, 1 = errors, 2 = warnings with --strict</para>
/// </remarks>
public class ValidateOutputPhase : ICommandPhase
{
    /// <summary>
    /// Exit code when validation passes (no errors, warnings OK).
    /// </summary>
    public const int ExitSuccess = 0;

    /// <summary>
    /// Exit code when validation finds errors.
    /// </summary>
    public const int ExitErrors = 1;

    /// <summary>
    /// Exit code when validation finds warnings with --strict mode.
    /// </summary>
    public const int ExitWarnings = 2;

    /// <inheritdoc />
    public Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var results = context.Get<IReadOnlyList<FileValidationResult>>(ContextKeys.ValidationResults);
        if (results == null)
        {
            context.Console.WriteError("ValidationResults not set. Run ValidationPhase first.");
            return Task.FromResult(1);
        }

        var quiet = context.Get<bool>(ContextKeys.Quiet);
        var strict = context.Get<bool>(ContextKeys.Strict);

        // Calculate totals
        var totalSpecs = 0;
        var totalErrors = 0;
        var totalWarnings = 0;

        foreach (var result in results)
        {
            totalSpecs += result.SpecCount;
            totalErrors += result.Errors.Count;
            totalWarnings += result.Warnings.Count;
        }

        // Show header
        if (!quiet)
        {
            context.Console.WriteLine("Validating spec structure...\n");
        }

        // Output results
        OutputResults(context, results, quiet);

        // Output summary
        if (!quiet)
        {
            context.Console.WriteLine("");
            context.Console.WriteLine(new string('\u2501', 40)); // ━
            context.Console.WriteLine($"Files: {results.Count} | Specs: {totalSpecs} | Errors: {totalErrors} | Warnings: {totalWarnings}");
        }

        // Determine exit code
        if (totalErrors > 0)
            return Task.FromResult(ExitErrors);

        if (totalWarnings > 0 && strict)
            return Task.FromResult(ExitWarnings);

        return Task.FromResult(ExitSuccess);
    }

    private static void OutputResults(CommandContext context, IReadOnlyList<FileValidationResult> results, bool quiet)
    {
        foreach (var result in results)
        {
            if (result.Errors.Count > 0)
            {
                // File with errors
                context.Console.WriteError($"\u2717 {result.FilePath}");
                foreach (var error in result.Errors)
                {
                    var location = error.LineNumber.HasValue ? $"Line {error.LineNumber}: " : "";
                    context.Console.WriteError($"  {location}{error.Message}");
                }
            }
            else if (result.Warnings.Count > 0)
            {
                // File with warnings only
                if (!quiet)
                {
                    context.Console.WriteLine($"\u26a0 {result.FilePath} - {result.SpecCount} specs");
                    foreach (var warning in result.Warnings)
                    {
                        var location = warning.LineNumber.HasValue ? $"Line {warning.LineNumber}: " : "";
                        context.Console.WriteLine($"  {location}{warning.Message}");
                    }
                }
            }
            else
            {
                // Valid file
                if (!quiet)
                {
                    context.Console.WriteSuccess($"\u2713 {result.FilePath} - {result.SpecCount} specs");
                }
            }
        }
    }
}
