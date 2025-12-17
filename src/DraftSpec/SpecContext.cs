namespace DraftSpec;

/// <summary>
/// Represents a describe/context block containing specs, nested contexts, and lifecycle hooks.
/// </summary>
/// <remarks>
/// SpecContext forms a tree structure where each node can contain:
/// <list type="bullet">
/// <item><description>Child contexts (nested describe blocks)</description></item>
/// <item><description>Specs (it blocks)</description></item>
/// <item><description>Lifecycle hooks (beforeAll, afterAll, beforeEach, afterEach)</description></item>
/// </list>
/// </remarks>
public class SpecContext
{
    /// <summary>
    /// The description text for this context block.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The parent context, or null if this is the root context.
    /// </summary>
    public SpecContext? Parent { get; }

    // Internal mutable lists
    private readonly List<SpecContext> _children = [];
    private readonly List<SpecDefinition> _specs = [];

    /// <summary>
    /// Child contexts (read-only view). Use <see cref="AddChild"/> to add.
    /// </summary>
    public IReadOnlyList<SpecContext> Children => _children;

    /// <summary>
    /// Specs in this context (read-only view). Use <see cref="AddSpec"/> to add.
    /// </summary>
    public IReadOnlyList<SpecDefinition> Specs => _specs;

    /// <summary>
    /// Hook that runs once before any spec in this context. Runs after parent's BeforeAll.
    /// </summary>
    public Func<Task>? BeforeAll { get; set; }

    /// <summary>
    /// Hook that runs once after all specs in this context. Runs before parent's AfterAll.
    /// </summary>
    public Func<Task>? AfterAll { get; set; }

    /// <summary>
    /// Hook that runs before each spec. Runs after parent's BeforeEach.
    /// </summary>
    public Func<Task>? BeforeEach { get; set; }

    /// <summary>
    /// Hook that runs after each spec. Runs before parent's AfterEach.
    /// </summary>
    public Func<Task>? AfterEach { get; set; }

    // Cached hook chains for performance (computed lazily)
    private List<Func<Task>>? _beforeEachChain;
    private List<Func<Task>>? _afterEachChain;

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

    /// <summary>
    /// Adds a spec to this context.
    /// </summary>
    /// <param name="spec">The spec definition to add.</param>
    public void AddSpec(SpecDefinition spec)
    {
        _specs.Add(spec);
    }

    /// <summary>
    /// Adds a child context to this context.
    /// </summary>
    /// <param name="child">The child context to add.</param>
    public void AddChild(SpecContext child)
    {
        _children.Add(child);
    }

    /// <summary>
    /// Gets the cached beforeEach hook chain (parent to child order).
    /// </summary>
    internal IReadOnlyList<Func<Task>> GetBeforeEachChain()
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
    internal IReadOnlyList<Func<Task>> GetAfterEachChain()
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