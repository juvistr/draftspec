using DraftSpec.Configuration;
using DraftSpec.Scripting;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Result of executing specs from a single file.
/// </summary>
/// <param name="RelativeSourceFile">Relative path to the source file.</param>
/// <param name="AbsoluteSourceFile">Absolute path to the source file.</param>
/// <param name="Results">Spec results from execution.</param>
internal sealed record ExecutionResult(
    string RelativeSourceFile,
    string AbsoluteSourceFile,
    IReadOnlyList<SpecResult> Results);
