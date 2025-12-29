using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace DraftSpec.Cli;

/// <summary>
/// Formats compilation diagnostics with source context for enhanced error display.
/// Shows surrounding source lines with caret pointing to exact error location.
/// </summary>
public class CompilationDiagnosticFormatter : ICompilationDiagnosticFormatter
{
    private const int ContextLinesBefore = 3;
    private const int ContextLinesAfter = 3;

    private readonly IFileSystem _fileSystem;

    public CompilationDiagnosticFormatter(IFileSystem? fileSystem = null)
    {
        _fileSystem = fileSystem ?? new FileSystem();
    }

    /// <summary>
    /// Formats a CompilationErrorException with source context.
    /// </summary>
    /// <param name="exception">The compilation error exception.</param>
    /// <param name="useColors">Whether to use ANSI color codes.</param>
    /// <returns>Formatted error message with source context.</returns>
    public string Format(CompilationErrorException exception, bool useColors = true)
    {
        var sb = new StringBuilder();

        foreach (var diagnostic in exception.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
        {
            FormatDiagnostic(sb, diagnostic, useColors);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Formats a single diagnostic with source context.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to format.</param>
    /// <param name="useColors">Whether to use ANSI color codes.</param>
    /// <returns>Formatted diagnostic with source context.</returns>
    public string FormatDiagnostic(Diagnostic diagnostic, bool useColors = true)
    {
        var sb = new StringBuilder();
        FormatDiagnostic(sb, diagnostic, useColors);
        return sb.ToString().TrimEnd();
    }

    private void FormatDiagnostic(StringBuilder sb, Diagnostic diagnostic, bool useColors)
    {
        var location = diagnostic.Location;
        var lineSpan = location.GetLineSpan();

        if (!lineSpan.IsValid)
        {
            // No source location - just format the message
            FormatError(sb, diagnostic.GetMessage(), diagnostic.Id, useColors);
            return;
        }

        var filePath = lineSpan.Path;
        var line = lineSpan.StartLinePosition.Line + 1; // Convert 0-based to 1-based
        var column = lineSpan.StartLinePosition.Character + 1;

        // Format the error header
        FormatError(sb, $"{diagnostic.Id}: {diagnostic.GetMessage()}", $"Line {line}, Column {column}", useColors);
        sb.AppendLine();

        // Try to add source context
        if (!string.IsNullOrEmpty(filePath) && _fileSystem.FileExists(filePath))
        {
            FormatSourceContext(sb, filePath, line, column, useColors);
        }
    }

    private void FormatSourceContext(StringBuilder sb, string filePath, int errorLine, int errorColumn, bool useColors)
    {
        try
        {
            var content = _fileSystem.ReadAllText(filePath);
            var lines = content.Split('\n');

            // Calculate line range (1-based to 0-based conversion)
            var startLine = Math.Max(0, errorLine - 1 - ContextLinesBefore);
            var endLine = Math.Min(lines.Length - 1, errorLine - 1 + ContextLinesAfter);

            // Find max line number width for padding
            var maxLineNum = endLine + 1;
            var lineNumWidth = maxLineNum.ToString().Length;

            for (var i = startLine; i <= endLine; i++)
            {
                var lineNum = i + 1;
                var lineContent = lines[i].TrimEnd('\r');
                var isErrorLine = lineNum == errorLine;

                // Format line number
                var lineNumStr = lineNum.ToString().PadLeft(lineNumWidth);

                if (isErrorLine)
                {
                    // Error line - highlighted
                    if (useColors)
                        sb.Append(AnsiColors.Red);
                    sb.Append($"   {lineNumStr} | ");
                    sb.Append(lineContent);
                    if (useColors)
                        sb.Append(AnsiColors.Reset);
                    sb.AppendLine();

                    // Add caret line
                    var caretPadding = new string(' ', 3 + lineNumWidth + 3 + Math.Max(0, errorColumn - 1));
                    if (useColors)
                        sb.Append(AnsiColors.Red);
                    sb.Append(caretPadding);
                    sb.Append("^--- ");
                    if (useColors)
                        sb.Append(AnsiColors.Reset);
                }
                else
                {
                    // Context line - dimmed
                    if (useColors)
                        sb.Append(AnsiColors.Dim);
                    sb.Append($"   {lineNumStr} | {lineContent}");
                    if (useColors)
                        sb.Append(AnsiColors.Reset);
                    sb.AppendLine();
                }
            }
        }
        catch
        {
            // If we can't read the file, just skip source context
        }
    }

    private static void FormatError(StringBuilder sb, string message, string? context, bool useColors)
    {
        if (useColors)
            sb.Append(AnsiColors.Red);

        sb.Append("  ");
        sb.Append(message);

        if (!string.IsNullOrEmpty(context))
        {
            if (useColors)
            {
                sb.Append(AnsiColors.Reset);
                sb.Append(AnsiColors.Dim);
            }
            sb.Append($" ({context})");
        }

        if (useColors)
            sb.Append(AnsiColors.Reset);
    }
}

/// <summary>
/// ANSI escape codes for terminal colors.
/// </summary>
public static class AnsiColors
{
    public const string Reset = "\x1b[0m";
    public const string Red = "\x1b[31m";
    public const string Green = "\x1b[32m";
    public const string Yellow = "\x1b[33m";
    public const string Cyan = "\x1b[36m";
    public const string Dim = "\x1b[2m";
    public const string Bold = "\x1b[1m";
}
