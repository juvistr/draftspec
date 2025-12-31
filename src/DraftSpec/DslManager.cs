namespace DraftSpec;

/// <summary>
/// Default implementation that delegates to static Dsl.Reset().
/// </summary>
public class DslManager : IDslManager
{
    /// <summary>
    /// Reset DSL state via static Dsl.Reset().
    /// </summary>
    public void Reset() => Dsl.Reset();
}
