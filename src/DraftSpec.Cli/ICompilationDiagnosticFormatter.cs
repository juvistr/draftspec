using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace DraftSpec.Cli;

/// <summary>
/// Formats compilation diagnostics with source context for enhanced error display.
/// </summary>
public interface ICompilationDiagnosticFormatter
{
    /// <summary>
    /// Formats a CompilationErrorException with source context.
    /// </summary>
    /// <param name="exception">The compilation error exception.</param>
    /// <param name="useColors">Whether to use ANSI color codes.</param>
    /// <returns>Formatted error message with source context.</returns>
    string Format(CompilationErrorException exception, bool useColors = true);

    /// <summary>
    /// Formats a single diagnostic with source context.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to format.</param>
    /// <param name="useColors">Whether to use ANSI color codes.</param>
    /// <returns>Formatted diagnostic with source context.</returns>
    string FormatDiagnostic(Diagnostic diagnostic, bool useColors = true);
}
