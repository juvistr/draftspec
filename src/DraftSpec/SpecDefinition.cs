namespace DraftSpec;

/// <summary>
/// Represents an individual spec (an "it" block).
/// </summary>
public class SpecDefinition
{
    public string Description { get; }
    public Action? Body { get; }
    public bool IsPending => Body is null;
    public bool IsSkipped { get; init; }
    public bool IsFocused { get; init; }

    public SpecDefinition(string description, Action? body = null)
    {
        Description = description;
        Body = body;
    }
}
