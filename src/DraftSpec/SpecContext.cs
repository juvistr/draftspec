namespace DraftSpec;

/// <summary>
/// Represents a describe/context block containing specs and nested contexts.
/// </summary>
public class SpecContext
{
    public string Description { get; }
    public SpecContext? Parent { get; }
    public List<SpecContext> Children { get; } = [];
    public List<SpecDefinition> Specs { get; } = [];

    public Action? BeforeAll { get; set; }
    public Action? AfterAll { get; set; }
    public Action? BeforeEach { get; set; }
    public Action? AfterEach { get; set; }

    /// <summary>
    /// Creates a new spec context.
    /// </summary>
    /// <param name="description">Description for this context block (cannot be empty)</param>
    /// <param name="parent">Optional parent context</param>
    /// <exception cref="ArgumentException">Thrown when description is null or empty</exception>
    public SpecContext(string description, SpecContext? parent = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or empty", nameof(description));

        Description = description;
        Parent = parent;
        parent?.Children.Add(this);
    }

    public void AddSpec(SpecDefinition spec) => Specs.Add(spec);

    public void AddChild(SpecContext child) => Children.Add(child);
}
