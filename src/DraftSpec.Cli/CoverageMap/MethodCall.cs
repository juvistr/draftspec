namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// Represents a method call found in a spec body.
/// </summary>
public sealed class MethodCall
{
    /// <summary>
    /// The method name being called.
    /// Example: "CreateAsync"
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// The receiver expression or type name if available.
    /// Example: "service" or "TodoService"
    /// </summary>
    public string? ReceiverName { get; init; }

    /// <summary>
    /// Argument type names if determinable.
    /// </summary>
    public IReadOnlyList<string> ArgumentTypes { get; init; } = [];

    /// <summary>
    /// 1-based line number of the call.
    /// </summary>
    public int LineNumber { get; init; }
}
