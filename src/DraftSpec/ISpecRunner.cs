namespace DraftSpec;

/// <summary>
/// Interface for spec execution, enabling testing and custom implementations.
/// </summary>
public interface ISpecRunner
{
    /// <summary>
    /// Run all specs in the given spec class (synchronous wrapper).
    /// </summary>
    List<SpecResult> Run(Spec spec);

    /// <summary>
    /// Run all specs starting from the given root context (synchronous wrapper).
    /// </summary>
    List<SpecResult> Run(SpecContext rootContext);

    /// <summary>
    /// Run all specs in the given spec class asynchronously.
    /// </summary>
    Task<List<SpecResult>> RunAsync(Spec spec);

    /// <summary>
    /// Run all specs starting from the given root context asynchronously.
    /// </summary>
    Task<List<SpecResult>> RunAsync(SpecContext rootContext);
}
