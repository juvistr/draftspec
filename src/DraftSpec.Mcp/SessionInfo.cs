using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Information about a session for API responses.
/// </summary>
public record SessionInfo
{
    public required string Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastAccessedAt { get; init; }
    public int TimeoutMinutes { get; init; }
    public bool HasAccumulatedContent { get; init; }
    public bool IsExpired { get; init; }
}
