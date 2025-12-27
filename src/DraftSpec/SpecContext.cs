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
    /// Shared empty hook chain to avoid allocations for contexts without hooks.
    /// </summary>
    private static readonly IReadOnlyList<Func<Task>> EmptyHookChain = Array.Empty<Func<Task>>();

    /// <summary>
    /// The description text for this context block.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The parent context, or null if this is the root context.
    /// </summary>
    public SpecContext? Parent { get; }

    /// <summary>
    /// Line number in the source file where this context was defined.
    /// Used for IDE navigation support.
    /// </summary>
    public int LineNumber { get; init; }

    // Internal mutable lists
    private readonly List<SpecContext> _children = [];
    private readonly List<SpecDefinition> _specs = [];

    // Cached counts computed during tree construction
    private int _totalSpecCount;
    private bool _hasFocusedDescendants;

    /// <summary>
    /// Total number of specs in this context and all descendants.
    /// Computed incrementally during tree construction.
    /// </summary>
    public int TotalSpecCount => _totalSpecCount;

    /// <summary>
    /// Whether this context or any descendant contains a focused spec.
    /// Computed incrementally during tree construction.
    /// </summary>
    public bool HasFocusedDescendants => _hasFocusedDescendants;

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
    // Uses IReadOnlyList to allow either List<T> or Array.Empty<T> singleton
    private IReadOnlyList<Func<Task>>? _beforeEachChain;
    private IReadOnlyList<Func<Task>>? _afterEachChain;

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

        // Update cached counts
        IncrementSpecCount();

        if (spec.IsFocused)
        {
            PropagateHasFocused();
        }
    }

    /// <summary>
    /// Increments spec count in this context and all ancestors.
    /// </summary>
    private void IncrementSpecCount()
    {
        var current = this;
        while (current != null)
        {
            current._totalSpecCount++;
            current = current.Parent;
        }
    }

    /// <summary>
    /// Propagates HasFocusedDescendants flag up to all ancestors.
    /// </summary>
    private void PropagateHasFocused()
    {
        var current = this;
        while (current != null && !current._hasFocusedDescendants)
        {
            current._hasFocusedDescendants = true;
            current = current.Parent;
        }
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
            // Check if any hooks exist in the chain before allocating
            if (!HasBeforeEachHooksInChain())
            {
                _beforeEachChain = EmptyHookChain;
            }
            else
            {
                // Build in child-to-parent order (O(1) per Add), then reverse once (O(n))
                // This is O(n) total instead of O(n²) from Insert(0, ...) per item
                var chain = new List<Func<Task>>();
                var current = this;
                while (current != null)
                {
                    if (current.BeforeEach != null)
                        chain.Add(current.BeforeEach);
                    current = current.Parent;
                }

                chain.Reverse();
                _beforeEachChain = chain;
            }
        }

        return _beforeEachChain;
    }

    /// <summary>
    /// Checks if any BeforeEach hooks exist in this context or its ancestors.
    /// </summary>
    private bool HasBeforeEachHooksInChain()
    {
        var current = this;
        while (current != null)
        {
            if (current.BeforeEach != null)
                return true;
            current = current.Parent;
        }
        return false;
    }

    /// <summary>
    /// Gets the cached afterEach hook chain (child to parent order).
    /// </summary>
    internal IReadOnlyList<Func<Task>> GetAfterEachChain()
    {
        if (_afterEachChain == null)
        {
            // Check if any hooks exist in the chain before allocating
            if (!HasAfterEachHooksInChain())
            {
                _afterEachChain = EmptyHookChain;
            }
            else
            {
                var chain = new List<Func<Task>>();
                var current = this;
                while (current != null)
                {
                    if (current.AfterEach != null)
                        chain.Add(current.AfterEach);
                    current = current.Parent;
                }
                _afterEachChain = chain;
            }
        }

        return _afterEachChain;
    }

    /// <summary>
    /// Checks if any AfterEach hooks exist in this context or its ancestors.
    /// </summary>
    private bool HasAfterEachHooksInChain()
    {
        var current = this;
        while (current != null)
        {
            if (current.AfterEach != null)
                return true;
            current = current.Parent;
        }
        return false;
    }
}