using System.Text.RegularExpressions;
using DraftSpec.Mcp.Models;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Parses error output from spec execution into structured error information.
/// </summary>
public static partial class ErrorParser
{
    /// <summary>
    /// Parse error output and categorize it.
    /// </summary>
    /// <param name="stderr">Standard error output from the process</param>
    /// <param name="stdout">Standard output from the process (may contain errors)</param>
    /// <param name="exitCode">Process exit code</param>
    /// <param name="timedOut">Whether the execution timed out</param>
    /// <returns>Structured error information, or null if no error</returns>
    public static SpecError? Parse(string? stderr, string? stdout, int exitCode, bool timedOut)
    {
        if (timedOut)
        {
            return new SpecError
            {
                Category = ErrorCategory.Timeout,
                Message = "Execution timed out"
            };
        }

        if (exitCode == 0 && string.IsNullOrWhiteSpace(stderr))
        {
            return null;
        }

        var combinedOutput = $"{stderr}\n{stdout}";

        // Try to parse specific error types in order of specificity
        return TryParseCompilationError(stderr)
            ?? TryParseAssertionError(combinedOutput)
            ?? TryParseSetupError(combinedOutput)
            ?? TryParseTeardownError(combinedOutput)
            ?? TryParseConfigurationError(combinedOutput)
            ?? TryParseRuntimeError(stderr, stdout, exitCode);
    }

    /// <summary>
    /// Try to parse a C# compilation error.
    /// Format: path(line,col): error CS####: message
    /// </summary>
    private static SpecError? TryParseCompilationError(string? stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr))
            return null;

        var match = CompilationErrorPattern().Match(stderr);
        if (!match.Success)
            return null;

        return new SpecError
        {
            Category = ErrorCategory.Compilation,
            Message = match.Groups["message"].Value.Trim(),
            SourceFile = match.Groups["file"].Value,
            LineNumber = int.TryParse(match.Groups["line"].Value, out var line) ? line : null,
            ColumnNumber = int.TryParse(match.Groups["col"].Value, out var col) ? col : null,
            ErrorCode = match.Groups["code"].Value
        };
    }

    /// <summary>
    /// Try to parse an assertion error from DraftSpec's expect() API.
    /// Format: Expected {expression} to be {expected}, but was {actual}
    /// </summary>
    private static SpecError? TryParseAssertionError(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        // Check for AssertionException in stack trace
        if (!output.Contains("AssertionException", StringComparison.Ordinal) && !output.Contains("Expected", StringComparison.Ordinal))
            return null;

        var match = AssertionErrorPattern().Match(output);
        if (!match.Success)
        {
            // Fallback: look for simpler assertion pattern
            match = SimpleAssertionPattern().Match(output);
        }

        if (!match.Success)
            return null;

        var message = match.Groups["message"].Success
            ? match.Groups["message"].Value
            : match.Value;

        var expected = match.Groups["expected"].Success ? match.Groups["expected"].Value : null;
        var actual = match.Groups["actual"].Success ? match.Groups["actual"].Value : null;

        // Try to extract line number from stack trace
        var lineMatch = StackTraceLinePattern().Match(output);

        return new SpecError
        {
            Category = ErrorCategory.Assertion,
            Message = message.Trim(),
            ExpectedValue = expected,
            ActualValue = actual,
            LineNumber = lineMatch.Success && int.TryParse(lineMatch.Groups["line"].Value, out var line) ? line : null,
            StackTrace = ExtractStackTrace(output)
        };
    }

    /// <summary>
    /// Try to parse a setup error (beforeAll/before hook failure).
    /// </summary>
    private static SpecError? TryParseSetupError(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        if (!output.Contains("beforeAll", StringComparison.Ordinal) && !output.Contains("before(", StringComparison.Ordinal) && !output.Contains("Before", StringComparison.Ordinal))
            return null;

        // Check if it's in a hook context
        if (SetupErrorPattern().IsMatch(output))
        {
            var message = ExtractExceptionMessage(output) ?? "Setup hook failed";
            return new SpecError
            {
                Category = ErrorCategory.Setup,
                Message = message,
                StackTrace = ExtractStackTrace(output)
            };
        }

        return null;
    }

    /// <summary>
    /// Try to parse a teardown error (afterAll/after hook failure).
    /// </summary>
    private static SpecError? TryParseTeardownError(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        if (!output.Contains("afterAll", StringComparison.Ordinal) && !output.Contains("after(", StringComparison.Ordinal) && !output.Contains("After", StringComparison.Ordinal))
            return null;

        if (TeardownErrorPattern().IsMatch(output))
        {
            var message = ExtractExceptionMessage(output) ?? "Teardown hook failed";
            return new SpecError
            {
                Category = ErrorCategory.Teardown,
                Message = message,
                StackTrace = ExtractStackTrace(output)
            };
        }

        return null;
    }

    /// <summary>
    /// Try to parse a configuration error.
    /// </summary>
    private static SpecError? TryParseConfigurationError(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        // Check for common configuration issues
        if (output.Contains("describe", StringComparison.Ordinal) && output.Contains("cannot be nested", StringComparison.Ordinal) ||
            output.Contains("Invalid spec", StringComparison.Ordinal) ||
            output.Contains("Configuration error", StringComparison.Ordinal))
        {
            return new SpecError
            {
                Category = ErrorCategory.Configuration,
                Message = ExtractExceptionMessage(output) ?? "Invalid spec configuration"
            };
        }

        return null;
    }

    /// <summary>
    /// Parse a generic runtime error.
    /// </summary>
    private static SpecError? TryParseRuntimeError(string? stderr, string? stdout, int exitCode)
    {
        if (exitCode == 0)
            return null;

        var output = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
        if (string.IsNullOrWhiteSpace(output))
        {
            return new SpecError
            {
                Category = ErrorCategory.Runtime,
                Message = $"Process exited with code {exitCode}"
            };
        }

        var message = ExtractExceptionMessage(output) ?? output.Split('\n').FirstOrDefault()?.Trim() ?? "Unknown error";
        var lineMatch = StackTraceLinePattern().Match(output);

        return new SpecError
        {
            Category = ErrorCategory.Runtime,
            Message = message,
            LineNumber = lineMatch.Success && int.TryParse(lineMatch.Groups["line"].Value, out var line) ? line : null,
            StackTrace = ExtractStackTrace(output)
        };
    }

    /// <summary>
    /// Extract the exception message from output.
    /// </summary>
    private static string? ExtractExceptionMessage(string output)
    {
        // Look for "Exception: message" pattern
        var match = ExceptionMessagePattern().Match(output);
        if (match.Success)
            return match.Groups["message"].Value.Trim();

        // Look for "Error: message" pattern
        match = ErrorMessagePattern().Match(output);
        if (match.Success)
            return match.Groups["message"].Value.Trim();

        return null;
    }

    /// <summary>
    /// Extract stack trace from output.
    /// </summary>
    private static string? ExtractStackTrace(string output)
    {
        var lines = output.Split('\n');
        var stackLines = lines
            .SkipWhile(l => !l.TrimStart().StartsWith("at ", StringComparison.Ordinal))
            .TakeWhile(l => l.TrimStart().StartsWith("at ", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(l))
            .ToList();

        return stackLines.Count > 0 ? string.Join("\n", stackLines).Trim() : null;
    }

    // Regex patterns using source generators for performance
    // Patterns use NonBacktracking where compatible. For patterns with lookahead or
    // lazy quantifiers, the patterns are manually verified to not have ReDoS vulnerabilities
    // (no nested quantifiers that could cause exponential backtracking).

    /// <summary>
    /// Matches C# compilation errors: path(line,col): error CS####: message
    /// </summary>
    [GeneratedRegex(@"(?<file>[^(]+)\((?<line>\d+),(?<col>\d+)\):\s*error\s+(?<code>CS\d+):\s*(?<message>.+)", RegexOptions.Multiline | RegexOptions.NonBacktracking)]
    private static partial Regex CompilationErrorPattern();

    /// <summary>
    /// Matches DraftSpec assertion errors: Expected X to be Y, but was Z
    /// Uses lazy quantifiers - not compatible with NonBacktracking but safe (no nested quantifiers).
    /// </summary>
#pragma warning disable MA0009 // Pattern uses lazy quantifiers which are not compatible with NonBacktracking
    [GeneratedRegex(@"(?<message>Expected\s+(?<expression>.+?)\s+to\s+be\s+(?<expected>.+?),\s+but\s+was\s+(?<actual>.+))")]
    private static partial Regex AssertionErrorPattern();
#pragma warning restore MA0009

    /// <summary>
    /// Simple assertion pattern for other expect() failures.
    /// </summary>
    [GeneratedRegex(@"(?<message>Expected\s+.+)", RegexOptions.NonBacktracking)]
    private static partial Regex SimpleAssertionPattern();

    /// <summary>
    /// Matches line numbers in stack traces: :line ###
    /// </summary>
    [GeneratedRegex(@":line\s+(?<line>\d+)", RegexOptions.NonBacktracking)]
    private static partial Regex StackTraceLinePattern();

    /// <summary>
    /// Matches setup hook errors.
    /// </summary>
    [GeneratedRegex(@"(?:beforeAll|before\s*\(|Before.*failed)", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture)]
    private static partial Regex SetupErrorPattern();

    /// <summary>
    /// Matches teardown hook errors.
    /// </summary>
    [GeneratedRegex(@"(?:afterAll|after\s*\(|After.*failed)", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture)]
    private static partial Regex TeardownErrorPattern();

    /// <summary>
    /// Extracts exception message: ExceptionType: message
    /// Uses lookahead - not compatible with NonBacktracking but safe (no nested quantifiers).
    /// </summary>
#pragma warning disable MA0009 // Pattern uses lookahead which is not compatible with NonBacktracking
    [GeneratedRegex(@"(?<type>\w+Exception):\s*(?<message>.+?)(?=\r?\n\s*at\s|$)", RegexOptions.Singleline)]
    private static partial Regex ExceptionMessagePattern();
#pragma warning restore MA0009

    /// <summary>
    /// Extracts error message: error: message or Error: message
    /// Uses lookahead - not compatible with NonBacktracking but safe (no nested quantifiers).
    /// </summary>
#pragma warning disable MA0009 // Pattern uses lookahead which is not compatible with NonBacktracking
    [GeneratedRegex(@"[Ee]rror:\s*(?<message>.+?)(?=\r?\n|$)")]
    private static partial Regex ErrorMessagePattern();
#pragma warning restore MA0009
}
