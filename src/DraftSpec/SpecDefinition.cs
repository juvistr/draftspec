namespace DraftSpec;

/// <summary>
/// Represents an individual spec (an "it" block).
/// </summary>
public class SpecDefinition
{
    public string Description { get; }

    /// <summary>
    /// The spec body. Returns a Task to support async specs.
    /// Sync specs are wrapped to return Task.CompletedTask.
    /// </summary>
    public Func<Task>? Body { get; }

    public bool IsPending => Body is null;
    public bool IsSkipped { get; init; }
    public bool IsFocused { get; init; }

    /// <summary>
    /// Tags associated with this spec for filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Create a spec with an async body.
    /// </summary>
    public SpecDefinition(string description, Func<Task>? body = null)
    {
        Description = description;
        Body = body;
    }

    /// <summary>
    /// Create a spec with a sync body (wrapped to return Task.CompletedTask).
    /// </summary>
    public SpecDefinition(string description, Action body)
    {
        Description = description;
        Body = () => { body(); return Task.CompletedTask; };
    }
}
