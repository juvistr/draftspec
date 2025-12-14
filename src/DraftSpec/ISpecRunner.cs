namespace DraftSpec;

/// <summary>
/// Interface for spec execution, enabling testing and custom implementations.
/// </summary>
public interface ISpecRunner
{
    /// <summary>
    /// Run all specs in the given spec class.
    /// </summary>
    List<SpecResult> Run(Spec spec);

    /// <summary>
    /// Run all specs starting from the given root context.
    /// </summary>
    List<SpecResult> Run(SpecContext rootContext);
}
