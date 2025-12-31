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
}
