namespace DraftSpec.Cli;

/// <summary>
/// Factory for creating IInProcessSpecRunner instances with filter options.
/// </summary>
public interface IInProcessSpecRunnerFactory
{
    /// <summary>
    /// Create a spec runner with the specified filter options.
    /// </summary>
    IInProcessSpecRunner Create(
        string? filterTags = null,
        string? excludeTags = null,
        string? filterName = null,
        string? excludeName = null,
        IReadOnlyList<string>? filterContext = null,
        IReadOnlyList<string>? excludeContext = null);
}
