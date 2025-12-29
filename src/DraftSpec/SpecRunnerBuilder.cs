using System.Text.RegularExpressions;
using DraftSpec.Configuration;
using DraftSpec.Coverage;
using DraftSpec.Middleware;

namespace DraftSpec;

/// <summary>
/// Fluent builder for configuring SpecRunner with middleware.
/// </summary>
public class SpecRunnerBuilder
{
    /// <summary>
    /// Default timeout for regex operations to prevent ReDoS attacks.
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private readonly List<ISpecMiddleware> _middleware = [];
    private DraftSpecConfiguration? _configuration;
    private int _maxDegreeOfParallelism;
    private bool _bail;
    private ICoverageTracker? _coverageTracker;
    private CoverageIndex? _coverageIndex;

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
        var regex = new Regex(pattern, options, RegexTimeout);
        return Use(new FilterMiddleware(ctx =>
            {
                var fullDescription = string.Join(" ", ctx.ContextPath.Append(ctx.Spec.Description));
                return regex.IsMatch(fullDescription);
            }, $"does not match pattern '{pattern}'"));
    }

    /// <summary>
    /// Add filter middleware that excludes spec names matching a regex pattern.
    /// Matches against the full description (context path + spec description).
    /// </summary>
    /// <param name="pattern">Regex pattern to exclude</param>
    /// <param name="options">Regex options (default: IgnoreCase)</param>
    public SpecRunnerBuilder WithNameExcludeFilter(string pattern, RegexOptions options = RegexOptions.IgnoreCase)
    {
        var regex = new Regex(pattern, options, RegexTimeout);
        return Use(new FilterMiddleware(ctx =>
            {
                var fullDescription = string.Join(" ", ctx.ContextPath.Append(ctx.Spec.Description));
                return !regex.IsMatch(fullDescription);
            }, $"matches excluded pattern '{pattern}'"));
    }

    /// <summary>
    /// Add filter middleware that matches specs by context path patterns.
    /// Multiple patterns are combined with OR logic (spec runs if ANY pattern matches).
    /// </summary>
    /// <param name="patterns">Glob-style patterns with / separator. Supports * (single segment) and ** (any segments).</param>
    public SpecRunnerBuilder WithContextFilter(params string[] patterns)
    {
        if (patterns.Length == 0)
            throw new ArgumentException("At least one pattern must be specified", nameof(patterns));

        var regexes = patterns.Select(p => new Regex(
            ContextPatternToRegex(p),
            RegexOptions.IgnoreCase,
            RegexTimeout)).ToList();

        return Use(new FilterMiddleware(ctx =>
            {
                var contextPathStr = string.Join("/", ctx.ContextPath);
                return regexes.Any(r => r.IsMatch(contextPathStr));
            }, $"context does not match patterns: {string.Join(", ", patterns)}"));
    }

    /// <summary>
    /// Add filter middleware that excludes specs matching context path patterns.
    /// Multiple patterns are combined with OR logic (spec is excluded if ANY pattern matches).
    /// </summary>
    /// <param name="patterns">Glob-style patterns with / separator. Supports * (single segment) and ** (any segments).</param>
    public SpecRunnerBuilder WithContextExcludeFilter(params string[] patterns)
    {
        if (patterns.Length == 0)
            throw new ArgumentException("At least one pattern must be specified", nameof(patterns));

        var regexes = patterns.Select(p => new Regex(
            ContextPatternToRegex(p),
            RegexOptions.IgnoreCase,
            RegexTimeout)).ToList();

        return Use(new FilterMiddleware(ctx =>
            {
                var contextPathStr = string.Join("/", ctx.ContextPath);
                return !regexes.Any(r => r.IsMatch(contextPathStr));
            }, $"context matches excluded patterns: {string.Join(", ", patterns)}"));
    }

    /// <summary>
    /// Convert a glob-style context pattern to a regex pattern.
    /// Supports:
    /// - * matches a single path segment (anything except /)
    /// - ** matches any number of path segments (including zero)
    /// - Literal text is escaped
    /// </summary>
    private static string ContextPatternToRegex(string pattern)
    {
        // Split by ** first (matches any number of segments)
        var parts = pattern.Split(["**"], StringSplitOptions.None);
        var regexParts = new List<string>();

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];

            // Handle single * within each part (matches single segment)
            var subParts = part.Split('*');
            var subRegex = string.Join("[^/]*", subParts.Select(Regex.Escape));
            regexParts.Add(subRegex);
        }

        // Join with .* (matches any number of segments including /)
        var fullPattern = string.Join(".*", regexParts);

        // Pattern should match anywhere in the context path (partial match)
        // Remove leading/trailing slashes for cleaner matching
        fullPattern = fullPattern.Trim('/');

        return fullPattern;
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
    /// Enable per-spec coverage tracking.
    /// </summary>
    /// <param name="tracker">
    /// Coverage tracker implementation. If null, creates a new InProcessCoverageTracker.
    /// </param>
    /// <param name="index">
    /// Optional coverage index for reverse lookups (which specs cover which lines).
    /// </param>
    /// <remarks>
    /// Coverage middleware is added at the beginning of the pipeline to capture
    /// coverage from all other middleware and the spec itself.
    ///
    /// The tracker must be started before running specs and stopped after.
    /// </remarks>
    public SpecRunnerBuilder WithCoverage(ICoverageTracker? tracker = null, CoverageIndex? index = null)
    {
        _coverageTracker = tracker ?? new InProcessCoverageTracker();
        _coverageIndex = index;
        return this;
    }

    /// <summary>
    /// Get the coverage tracker, if configured.
    /// </summary>
    public ICoverageTracker? CoverageTracker => _coverageTracker;

    /// <summary>
    /// Get the coverage index, if configured.
    /// </summary>
    public CoverageIndex? CoverageIndex => _coverageIndex;

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

        // Add coverage middleware at the beginning if configured
        // This ensures it captures coverage from all other middleware
        var middleware = _middleware.ToList();
        if (_coverageTracker != null)
        {
            middleware.Insert(0, new CoverageMiddleware(_coverageTracker, _coverageIndex));
        }

        return new SpecRunner(middleware, _configuration, _maxDegreeOfParallelism, _bail);
    }
}