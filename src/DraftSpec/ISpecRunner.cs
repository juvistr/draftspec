namespace DraftSpec;

/// <summary>
/// Interface for spec execution, enabling testing and custom implementations.
/// </summary>
/// <remarks>
/// The default implementation is <see cref="SpecRunner"/>. Use <see cref="SpecRunnerBuilder"/>
/// to configure middleware like retry, timeout, and filtering.
/// </remarks>
public interface ISpecRunner
{
    /// <summary>
    /// Runs all specs in the given spec class synchronously.
    /// </summary>
    /// <param name="spec">The spec class instance containing the spec definitions.</param>
    /// <returns>List of results for each executed spec.</returns>
    List<SpecResult> Run(Spec spec);

    /// <summary>
    /// Runs all specs starting from the given root context synchronously.
    /// </summary>
    /// <param name="rootContext">The root context of the spec tree.</param>
    /// <returns>List of results for each executed spec.</returns>
    List<SpecResult> Run(SpecContext rootContext);

    /// <summary>
    /// Runs all specs in the given spec class asynchronously.
    /// </summary>
    /// <param name="spec">The spec class instance containing the spec definitions.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task that resolves to a list of results for each executed spec.</returns>
    Task<List<SpecResult>> RunAsync(Spec spec, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs all specs starting from the given root context asynchronously.
    /// </summary>
    /// <param name="rootContext">The root context of the spec tree.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task that resolves to a list of results for each executed spec.</returns>
    Task<List<SpecResult>> RunAsync(SpecContext rootContext, CancellationToken cancellationToken = default);
}
