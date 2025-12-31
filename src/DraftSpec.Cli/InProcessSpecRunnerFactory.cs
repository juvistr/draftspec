namespace DraftSpec.Cli;

/// <summary>
/// Default factory that creates InProcessSpecRunner instances.
/// </summary>
public class InProcessSpecRunnerFactory : IInProcessSpecRunnerFactory
{
    public IInProcessSpecRunner Create(
        string? filterTags = null,
        string? excludeTags = null,
        string? filterName = null,
        string? excludeName = null,
        IReadOnlyList<string>? filterContext = null,
        IReadOnlyList<string>? excludeContext = null)
    {
        return new InProcessSpecRunner(filterTags, excludeTags, filterName, excludeName, filterContext, excludeContext);
    }
}
