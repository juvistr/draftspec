using DraftSpec.Cli;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IInProcessSpecRunnerFactory for testing.
/// </summary>
public class MockInProcessSpecRunnerFactory : IInProcessSpecRunnerFactory
{
    private readonly MockInProcessSpecRunner _runner;
    private Exception? _exception;

    public List<CreateCall> CreateCalls { get; } = [];

    public record CreateCall(
        string? FilterTags,
        string? ExcludeTags,
        string? FilterName,
        string? ExcludeName,
        IReadOnlyList<string>? FilterContext,
        IReadOnlyList<string>? ExcludeContext);

    public MockInProcessSpecRunnerFactory()
    {
        _runner = new MockInProcessSpecRunner();
    }

    public MockInProcessSpecRunnerFactory(MockInProcessSpecRunner runner)
    {
        _runner = runner;
    }

    /// <summary>
    /// Gets the runner that will be returned by Create.
    /// </summary>
    public MockInProcessSpecRunner Runner => _runner;

    /// <summary>
    /// Configure the factory to throw on Create.
    /// </summary>
    public MockInProcessSpecRunnerFactory Throws(Exception exception)
    {
        _exception = exception;
        return this;
    }

    public IInProcessSpecRunner Create(
        string? filterTags = null,
        string? excludeTags = null,
        string? filterName = null,
        string? excludeName = null,
        IReadOnlyList<string>? filterContext = null,
        IReadOnlyList<string>? excludeContext = null)
    {
        CreateCalls.Add(new CreateCall(
            filterTags, excludeTags, filterName, excludeName,
            filterContext, excludeContext));

        if (_exception != null)
            throw _exception;

        return _runner;
    }
}
