using DraftSpec.TestingPlatform;
using Microsoft.CodeAnalysis.Scripting;

namespace DraftSpec.Cli;

/// <summary>
/// Exception that wraps compilation errors with enhanced diagnostic information
/// and discovered specs from static parsing.
/// </summary>
public class CompilationDiagnosticException : Exception
{
    /// <summary>
    /// The formatted error message with source context.
    /// </summary>
    public string FormattedMessage { get; }

    /// <summary>
    /// The file that failed to compile.
    /// </summary>
    public string SpecFile { get; }

    /// <summary>
    /// Specs discovered via static parsing despite the compilation error.
    /// </summary>
    public IReadOnlyList<StaticSpec> DiscoveredSpecs { get; }

    /// <summary>
    /// The original compilation error exception.
    /// </summary>
    public CompilationErrorException? CompilationError { get; }

    public CompilationDiagnosticException(
        string message,
        string formattedMessage,
        string specFile,
        IReadOnlyList<StaticSpec> discoveredSpecs,
        CompilationErrorException? compilationError = null)
        : base(message)
    {
        FormattedMessage = formattedMessage;
        SpecFile = specFile;
        DiscoveredSpecs = discoveredSpecs;
        CompilationError = compilationError;
    }
}
