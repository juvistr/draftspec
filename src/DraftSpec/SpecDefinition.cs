namespace DraftSpec;

/// <summary>
/// Represents an individual spec (an "it" block) with its description, body, and metadata.
/// </summary>
/// <remarks>
/// A spec can be in one of several states:
/// <list type="bullet">
/// <item><description>Normal: has a body and will be executed</description></item>
/// <item><description>Pending: no body defined (placeholder for future implementation)</description></item>
/// <item><description>Skipped: explicitly marked to be skipped via xit()</description></item>
/// <item><description>Focused: marked with fit() to run exclusively</description></item>
/// </list>
/// </remarks>
public class SpecDefinition
{
    /// <summary>
    /// The description text for this spec (the "it should..." part).
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The spec body to execute. Returns a Task to support async specs.
    /// Sync specs are wrapped to return Task.CompletedTask.
    /// Null if the spec is pending (no implementation yet).
    /// </summary>
    public Func<Task>? Body { get; }

    /// <summary>
    /// True if this spec has no body and should be marked as pending.
    /// </summary>
    public bool IsPending => Body is null;

    /// <summary>
    /// True if this spec should be skipped (via xit()).
    /// </summary>
    public bool IsSkipped { get; init; }

    /// <summary>
    /// True if this spec is focused (via fit()). When any spec is focused,
    /// only focused specs will run.
    /// </summary>
    public bool IsFocused { get; init; }

    /// <summary>
    /// Tags associated with this spec for filtering (e.g., "unit", "integration", "slow").
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Line number in the source file where this spec was defined.
    /// Used for IDE navigation support.
    /// </summary>
    public int LineNumber { get; init; }

    /// <summary>
    /// Creates a spec with an async body.
    /// </summary>
    /// <param name="description">Description of what the spec tests.</param>
    /// <param name="body">Async function to execute, or null for pending spec.</param>
    public SpecDefinition(string description, Func<Task>? body = null)
    {
        Description = description;
        Body = body;
    }

    /// <summary>
    /// Creates a spec with a synchronous body (wrapped to return Task.CompletedTask).
    /// </summary>
    /// <param name="description">Description of what the spec tests.</param>
    /// <param name="body">Synchronous action to execute.</param>
    public SpecDefinition(string description, Action body)
    {
        Description = description;
        Body = () =>
        {
            body();
            return Task.CompletedTask;
        };
    }
}
