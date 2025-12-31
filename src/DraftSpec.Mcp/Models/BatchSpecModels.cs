namespace DraftSpec.Mcp.Models;

/// <summary>
/// Input for a single spec in a batch execution.
/// </summary>
public class BatchSpecInput
{
    /// <summary>
    /// Name or identifier for this spec (used in results mapping).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The spec content using describe/it/expect syntax.
    /// </summary>
    public required string Content { get; init; }
}
