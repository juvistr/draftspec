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
    private Dictionary<string, Func<object>>? _letDefinitions;

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

    private List<Func<Task>>? _beforeAllHooks;
    private List<Func<Task>>? _afterAllHooks;
    private List<Func<Task>>? _beforeEachHooks;
    private List<Func<Task>>? _afterEachHooks;

    /// <summary>
    /// Hooks that run once before any spec in this context, in declaration order.
    /// </summary>
    internal IReadOnlyList<Func<Task>> BeforeAllHooks =>
        _beforeAllHooks ?? (IReadOnlyList<Func<Task>>)Array.Empty<Func<Task>>();

    /// <summary>
    /// Hooks that run once after all specs in this context.
    /// Runner executes these in reverse (LIFO) order.
    /// </summary>
    internal IReadOnlyList<Func<Task>> AfterAllHooks =>
        _afterAllHooks ?? (IReadOnlyList<Func<Task>>)Array.Empty<Func<Task>>();

    internal void AddBeforeAll(Func<Task> hook) => (_beforeAllHooks ??= []).Add(hook);
    internal void AddAfterAll(Func<Task> hook) => (_afterAllHooks ??= []).Add(hook);
    internal void AddBeforeEach(Func<Task> hook) { (_beforeEachHooks ??= []).Add(hook); _beforeEachChain = null; }
    internal void AddAfterEach(Func<Task> hook) { (_afterEachHooks ??= []).Add(hook); _afterEachChain = null; }

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
    /// Thread-safe via Interlocked.CompareExchange for lazy initialization.
    /// </summary>
    internal IReadOnlyList<Func<Task>> GetBeforeEachChain()
    {
        // Fast path: already computed
        var cached = _beforeEachChain;
        if (cached != null)
            return cached;

        // Compute the chain
        IReadOnlyList<Func<Task>> chain;
        if (!HasBeforeEachHooksInChain())
        {
            chain = EmptyHookChain;
        }
        else
        {
            // Build in child-to-parent order with each context's hooks reversed,
            // then reverse the whole list once. This yields parent-FIFO → child-FIFO order.
            var list = new List<Func<Task>>();
            var current = this;
            while (current != null)
            {
                if (current._beforeEachHooks != null)
                {
                    for (var i = current._beforeEachHooks.Count - 1; i >= 0; i--)
                        list.Add(current._beforeEachHooks[i]);
                }
                current = current.Parent;
            }

            list.Reverse();
            chain = list;
        }

        // Thread-safe assignment: if another thread set it first, use their value
        Interlocked.CompareExchange(ref _beforeEachChain, chain, null);
        return _beforeEachChain!;
    }

    /// <summary>
    /// Checks if any BeforeEach hooks exist in this context or its ancestors.
    /// </summary>
    private bool HasBeforeEachHooksInChain()
    {
        var current = this;
        while (current != null)
        {
            if (current._beforeEachHooks is { Count: > 0 })
                return true;
            current = current.Parent;
        }
        return false;
    }

    /// <summary>
    /// Gets the cached afterEach hook chain (child to parent order).
    /// Thread-safe via Interlocked.CompareExchange for lazy initialization.
    /// </summary>
    internal IReadOnlyList<Func<Task>> GetAfterEachChain()
    {
        // Fast path: already computed
        var cached = _afterEachChain;
        if (cached != null)
            return cached;

        // Compute the chain
        IReadOnlyList<Func<Task>> chain;
        if (!HasAfterEachHooksInChain())
        {
            chain = EmptyHookChain;
        }
        else
        {
            // Child-first, LIFO within each context: child hooks reversed, then parent hooks reversed
            var list = new List<Func<Task>>();
            var current = this;
            while (current != null)
            {
                if (current._afterEachHooks != null)
                {
                    for (var i = current._afterEachHooks.Count - 1; i >= 0; i--)
                        list.Add(current._afterEachHooks[i]);
                }
                current = current.Parent;
            }
            chain = list;
        }

        // Thread-safe assignment: if another thread set it first, use their value
        Interlocked.CompareExchange(ref _afterEachChain, chain, null);
        return _afterEachChain!;
    }

    /// <summary>
    /// Checks if any AfterEach hooks exist in this context or its ancestors.
    /// </summary>
    private bool HasAfterEachHooksInChain()
    {
        var current = this;
        while (current != null)
        {
            if (current._afterEachHooks is { Count: > 0 })
                return true;
            current = current.Parent;
        }
        return false;
    }

    /// <summary>
    /// Adds a let definition to this context.
    /// </summary>
    /// <param name="name">The name of the fixture</param>
    /// <param name="factory">Factory function that creates the value</param>
    internal void AddLetDefinition(string name, Func<object> factory)
    {
        _letDefinitions ??= new Dictionary<string, Func<object>>();
        _letDefinitions[name] = factory;
    }

    /// <summary>
    /// Gets the factory for a let definition, searching this context and ancestors.
    /// </summary>
    /// <param name="name">The name of the fixture</param>
    /// <returns>The factory function, or null if not found</returns>
    internal Func<object>? GetLetFactory(string name)
    {
        var current = this;
        while (current != null)
        {
            if (current._letDefinitions?.TryGetValue(name, out var factory) == true)
                return factory;
            current = current.Parent;
        }
        return null;
    }
}
