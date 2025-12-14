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

    public SpecContext(string description, SpecContext? parent = null)
    {
        Description = description;
        Parent = parent;
        parent?.Children.Add(this);
    }

    public void AddSpec(SpecDefinition spec) => Specs.Add(spec);

    public void AddChild(SpecContext child) => Children.Add(child);
}
