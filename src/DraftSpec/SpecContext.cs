namespace DraftSpec;

/// <summary>
/// Represents a describe/context block containing specs and nested contexts.
/// </summary>
public class SpecContext
{
    public string Description { get; }
    public SpecContext? Parent { get; }

    // Internal mutable lists
    private readonly List<SpecContext> _children = [];
    private readonly List<SpecDefinition> _specs = [];

    /// <summary>
    /// Child contexts (read-only view).
    /// </summary>
    public IReadOnlyList<SpecContext> Children => _children;

    /// <summary>
    /// Specs in this context (read-only view).
    /// </summary>
    public IReadOnlyList<SpecDefinition> Specs => _specs;

    public Action? BeforeAll { get; set; }
    public Action? AfterAll { get; set; }
    public Action? BeforeEach { get; set; }
    public Action? AfterEach { get; set; }

    // Cached hook chains for performance (computed lazily)
    private List<Action>? _beforeEachChain;
    private List<Action>? _afterEachChain;

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
        parent?._children.Add(this);
    }

    public void AddSpec(SpecDefinition spec) => _specs.Add(spec);

    public void AddChild(SpecContext child) => _children.Add(child);

    /// <summary>
    /// Gets the cached beforeEach hook chain (parent to child order).
    /// </summary>
    internal IReadOnlyList<Action> GetBeforeEachChain()
    {
        if (_beforeEachChain == null)
        {
            _beforeEachChain = [];
            var current = this;
            while (current != null)
            {
                if (current.BeforeEach != null)
                    _beforeEachChain.Insert(0, current.BeforeEach);
                current = current.Parent;
            }
        }
        return _beforeEachChain;
    }

    /// <summary>
    /// Gets the cached afterEach hook chain (child to parent order).
    /// </summary>
    internal IReadOnlyList<Action> GetAfterEachChain()
    {
        if (_afterEachChain == null)
        {
            _afterEachChain = [];
            var current = this;
            while (current != null)
            {
                if (current.AfterEach != null)
                    _afterEachChain.Add(current.AfterEach);
                current = current.Parent;
            }
        }
        return _afterEachChain;
    }
}
