namespace DraftSpec.Mcp.Services;

/// <summary>
/// Represents a persistent session for multi-turn spec execution workflows.
/// Sessions maintain accumulated spec content and shared state across multiple run_spec calls.
/// </summary>
public class Session : IDisposable
{
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Unique identifier for this session.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// When the session was last accessed.
    /// </summary>
    public DateTime LastAccessedAt { get; private set; }

    /// <summary>
    /// Session timeout duration. Session expires if not accessed within this period.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Accumulated spec content from previous runs in this session.
    /// This content is prepended to each new spec run.
    /// </summary>
    public string AccumulatedContent { get; private set; } = "";

    /// <summary>
    /// Session-specific temporary directory for spec files.
    /// </summary>
    public string TempDirectory { get; }

    /// <summary>
    /// Whether this session has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow - LastAccessedAt > Timeout;

    /// <summary>
    /// Create a new session.
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="timeout">Session timeout</param>
    /// <param name="tempDirectory">Temp directory for this session</param>
    public Session(string id, TimeSpan timeout, string tempDirectory)
    {
        Id = id;
        Timeout = timeout;
        TempDirectory = tempDirectory;
        CreatedAt = DateTime.UtcNow;
        LastAccessedAt = DateTime.UtcNow;

        // Ensure temp directory exists
        Directory.CreateDirectory(tempDirectory);
    }

    /// <summary>
    /// Touch the session to update last accessed time.
    /// </summary>
    public void Touch()
    {
        lock (_lock)
        {
            LastAccessedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Append content to the accumulated spec content.
    /// </summary>
    /// <param name="content">Content to append</param>
    public void AppendContent(string content)
    {
        lock (_lock)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                if (!string.IsNullOrEmpty(AccumulatedContent))
                {
                    AccumulatedContent += Environment.NewLine + Environment.NewLine;
                }
                AccumulatedContent += content;
            }
            LastAccessedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Get the full spec content including accumulated content.
    /// </summary>
    /// <param name="newContent">New content to run</param>
    /// <returns>Combined content</returns>
    public string GetFullContent(string newContent)
    {
        lock (_lock)
        {
            LastAccessedAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(AccumulatedContent))
            {
                return newContent;
            }
            return AccumulatedContent + Environment.NewLine + Environment.NewLine + newContent;
        }
    }

    /// <summary>
    /// Clear accumulated content.
    /// </summary>
    public void ClearContent()
    {
        lock (_lock)
        {
            AccumulatedContent = "";
            LastAccessedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Dispose the session and clean up resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Clean up temp directory
        try
        {
            if (Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        GC.SuppressFinalize(this);
    }
}
