using System.Text.RegularExpressions;
using DraftSpec.Configuration;
using DraftSpec.Middleware;

namespace DraftSpec;

/// <summary>
/// Fluent builder for configuring SpecRunner with middleware.
/// </summary>
public class SpecRunnerBuilder
{
    private readonly List<ISpecMiddleware> _middleware = [];
    private DraftSpecConfiguration? _configuration;
    private int _maxDegreeOfParallelism;
    private bool _bail;

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
    /// Add filter middleware with a custom predicate.
    /// </summary>
    /// <param name="predicate">Function that returns true if the spec should run</param>
    public SpecRunnerBuilder WithFilter(Func<SpecExecutionContext, bool> predicate)
    {
        return Use(new FilterMiddleware(predicate));
    }

    /// <summary>
    /// Add filter middleware that matches spec names against a regex pattern.
    /// Matches against the full description (context path + spec description).
    /// </summary>
    /// <param name="pattern">Regex pattern to match</param>
    /// <param name="options">Regex options (default: IgnoreCase)</param>
    public SpecRunnerBuilder WithNameFilter(string pattern, RegexOptions options = RegexOptions.IgnoreCase)
    {
        var regex = new Regex(pattern, options);
        return Use(new FilterMiddleware(ctx =>
            {
                var fullDescription = string.Join(" ", ctx.ContextPath.Append(ctx.Spec.Description));
                return regex.IsMatch(fullDescription);
            }, $"does not match pattern '{pattern}'"));
    }

    /// <summary>
    /// Add filter middleware that runs only specs with any of the specified tags.
    /// </summary>
    /// <param name="tags">Tags to match (spec runs if it has ANY of these tags)</param>
    public SpecRunnerBuilder WithTagFilter(params string[] tags)
    {
        if (tags.Length == 0)
            throw new ArgumentException("At least one tag must be specified", nameof(tags));

        var tagSet = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
        return Use(new FilterMiddleware(ctx =>
                ctx.Spec.Tags.Any(t => tagSet.Contains(t)),
            $"does not have tags: {string.Join(", ", tags)}"));
    }

    /// <summary>
    /// Add filter middleware that excludes specs with any of the specified tags.
    /// </summary>
    /// <param name="tags">Tags to exclude (spec skipped if it has ANY of these tags)</param>
    public SpecRunnerBuilder WithoutTags(params string[] tags)
    {
        if (tags.Length == 0)
            throw new ArgumentException("At least one tag must be specified", nameof(tags));

        var tagSet = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
        return Use(new FilterMiddleware(ctx =>
                !ctx.Spec.Tags.Any(t => tagSet.Contains(t)),
            $"has excluded tags: {string.Join(", ", tags)}"));
    }

    /// <summary>
    /// Set the configuration to use.
    /// </summary>
    /// <param name="configuration">The DraftSpec configuration</param>
    public SpecRunnerBuilder WithConfiguration(DraftSpecConfiguration configuration)
    {
        _configuration = configuration;
        return this;
    }

    /// <summary>
    /// Enable parallel execution of specs within contexts.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">
    /// Maximum number of specs to execute concurrently.
    /// 0 or negative uses Environment.ProcessorCount.
    /// 1 disables parallel execution.
    /// </param>
    /// <remarks>
    /// Parallel execution runs specs within the same context concurrently.
    /// BeforeAll/AfterAll hooks are still executed sequentially at context boundaries.
    /// BeforeEach/AfterEach hooks run per-spec (each spec gets its own hook execution).
    /// Results maintain original declaration order regardless of execution order.
    /// </remarks>
    public SpecRunnerBuilder WithParallelExecution(int maxDegreeOfParallelism = 0)
    {
        _maxDegreeOfParallelism = maxDegreeOfParallelism <= 0
            ? Environment.ProcessorCount
            : maxDegreeOfParallelism;
        return this;
    }

    /// <summary>
    /// Enable bail mode - stop execution after first failure.
    /// Remaining specs will be reported as skipped.
    /// </summary>
    public SpecRunnerBuilder WithBail()
    {
        _bail = true;
        return this;
    }

    /// <summary>
    /// Get the current configuration, or null if not set.
    /// </summary>
    internal DraftSpecConfiguration? Configuration => _configuration;

    /// <summary>
    /// Get the maximum degree of parallelism. 0 means sequential execution.
    /// </summary>
    internal int MaxDegreeOfParallelism => _maxDegreeOfParallelism;

    /// <summary>
    /// Get whether bail mode is enabled.
    /// </summary>
    internal bool Bail => _bail;

    /// <summary>
    /// Build the configured SpecRunner.
    /// </summary>
    public SpecRunner Build()
    {
        // Initialize configuration and let middleware plugins register
        if (_configuration != null)
        {
            _configuration.Initialize();
            _configuration.InitializeMiddleware(this);
        }

        return new SpecRunner(_middleware, _configuration, _maxDegreeOfParallelism, _bail);
    }
}