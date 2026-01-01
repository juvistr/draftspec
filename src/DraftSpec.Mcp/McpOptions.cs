namespace DraftSpec.Mcp;

/// <summary>
/// Configuration options for the MCP server.
/// </summary>
public class McpOptions
{
    /// <summary>
    /// Maximum number of concurrent spec executions.
    /// Default: 5.
    /// </summary>
    public int MaxConcurrentExecutions { get; init; } = 5;

    /// <summary>
    /// Maximum number of spec executions allowed per minute.
    /// Default: 60 (1 per second average).
    /// </summary>
    public int MaxExecutionsPerMinute { get; init; } = 60;

    /// <summary>
    /// Maximum size in bytes for spec content input.
    /// Default: 1MB. Prevents memory exhaustion from very large inputs.
    /// </summary>
    public int MaxSpecContentSizeBytes { get; init; } = 1_000_000;
}
