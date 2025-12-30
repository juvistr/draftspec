namespace DraftSpec.TestingPlatform;

/// <summary>
/// Default implementation of ISpecStateManager that delegates to static Dsl state.
/// </summary>
public class DefaultSpecStateManager : ISpecStateManager
{
    /// <inheritdoc />
    public void ResetState() => Dsl.Reset();

    /// <inheritdoc />
    public SpecContext? GetRootContext() => Dsl.RootContext;

    /// <inheritdoc />
    public void SetRootContext(SpecContext? context) => Dsl.RootContext = context;
}
