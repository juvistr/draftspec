using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of ISpecStateManager for testing.
/// Tracks all state operations for verification.
/// </summary>
public class MockSpecStateManager : ISpecStateManager
{
    private SpecContext? _rootContext;

    /// <summary>
    /// Number of times ResetState was called.
    /// </summary>
    public int ResetCallCount { get; private set; }

    /// <summary>
    /// Number of times GetRootContext was called.
    /// </summary>
    public int GetRootContextCallCount { get; private set; }

    /// <summary>
    /// Number of times SetRootContext was called.
    /// </summary>
    public int SetRootContextCallCount { get; private set; }

    /// <summary>
    /// History of contexts passed to SetRootContext.
    /// </summary>
    public List<SpecContext?> SetRootContextCalls { get; } = [];

    /// <summary>
    /// Configures the mock to return a specific root context.
    /// </summary>
    public MockSpecStateManager WithRootContext(SpecContext? context)
    {
        _rootContext = context;
        return this;
    }

    /// <inheritdoc />
    public void ResetState()
    {
        ResetCallCount++;
        _rootContext = null;
    }

    /// <inheritdoc />
    public SpecContext? GetRootContext()
    {
        GetRootContextCallCount++;
        return _rootContext;
    }

    /// <inheritdoc />
    public void SetRootContext(SpecContext? context)
    {
        SetRootContextCallCount++;
        SetRootContextCalls.Add(context);
        _rootContext = context;
    }
}
