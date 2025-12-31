using DraftSpec.Cli;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock compilation diagnostic formatter for unit testing.
/// Returns predictable formatted output for testing error handling paths.
/// </summary>
public class MockCompilationDiagnosticFormatter : ICompilationDiagnosticFormatter
{
    /// <summary>
    /// The message to return from Format().
    /// </summary>
    public string FormattedMessage { get; set; } = "Formatted compilation error";

    /// <summary>
    /// The message to return from FormatDiagnostic().
    /// </summary>
    public string DiagnosticMessage { get; set; } = "Formatted diagnostic";

    /// <summary>
    /// Count of times Format() was called.
    /// </summary>
    public int FormatCalls { get; private set; }

    /// <summary>
    /// Count of times FormatDiagnostic() was called.
    /// </summary>
    public int FormatDiagnosticCalls { get; private set; }

    public string Format(CompilationErrorException exception, bool useColors = true)
    {
        FormatCalls++;
        return FormattedMessage;
    }

    public string FormatDiagnostic(Diagnostic diagnostic, bool useColors = true)
    {
        FormatDiagnosticCalls++;
        return DiagnosticMessage;
    }
}
