using DraftSpec.Middleware;

namespace DraftSpec;

/// <summary>
/// Fluent builder for configuring SpecRunner with middleware.
/// </summary>
public class SpecRunnerBuilder
{
    private readonly List<ISpecMiddleware> _middleware = [];

    /// <summary>
    /// Add a middleware to the pipeline.
    /// </summary>
    public SpecRunnerBuilder Use(ISpecMiddleware middleware)
    {
        _middleware.Add(middleware);
        return this;
    }

    /// <summary>
    /// Add retry middleware.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retries</param>
    /// <param name="delayMs">Delay between retries in milliseconds (default: 0)</param>
    public SpecRunnerBuilder WithRetry(int maxRetries, int delayMs = 0)
    {
        return Use(new RetryMiddleware(maxRetries, TimeSpan.FromMilliseconds(delayMs)));
    }

    /// <summary>
    /// Add timeout middleware.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    public SpecRunnerBuilder WithTimeout(int timeoutMs)
    {
        return Use(new TimeoutMiddleware(timeoutMs));
    }

    /// <summary>
    /// Build the configured SpecRunner.
    /// </summary>
    public SpecRunner Build() => new(_middleware);
}
