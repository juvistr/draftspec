using System.Diagnostics;
using DraftSpec.Configuration;
using DraftSpec.Execution;
using DraftSpec.Middleware;
using DraftSpec.Plugins;
using DraftSpec.Snapshots;

namespace DraftSpec;

/// <summary>
/// Spec runner that walks the tree and executes specs through a middleware pipeline.
/// Supports both sequential and parallel execution of specs.
/// </summary>
public class SpecRunner : ISpecRunner
{
    private readonly IReadOnlyList<ISpecMiddleware> _middleware;
    private readonly Func<SpecExecutionContext, Task<SpecResult>> _pipeline;
    private readonly DraftSpecConfiguration? _configuration;
    private readonly ISpecExecutionStrategy _executionStrategy;
    private readonly IReadOnlyList<IReporter> _reporters;
    private readonly bool _bail;
    private volatile bool _bailTriggered;

    /// <summary>
    /// Create a SpecRunner with no middleware (default behavior).
    /// </summary>
    public SpecRunner() : this([], null, SequentialExecutionStrategy.Instance, false)
    {
    }

    /// <summary>
    /// Create a SpecRunner with the specified middleware.
    /// </summary>
    /// <param name="middleware">Middleware executed in order (first wraps last)</param>
    /// <param name="configuration">Optional configuration for reporters and formatters</param>
    /// <param name="maxDegreeOfParallelism">Maximum parallel spec execution (0 = sequential)</param>
    /// <param name="bail">Stop execution after first failure</param>
    public SpecRunner(
        IEnumerable<ISpecMiddleware> middleware,
        DraftSpecConfiguration? configuration = null,
        int maxDegreeOfParallelism = 0,
        bool bail = false)
        : this(
            middleware,
            configuration,
            maxDegreeOfParallelism > 1
                ? new ParallelExecutionStrategy(maxDegreeOfParallelism)
                : SequentialExecutionStrategy.Instance,
            bail)
    {
    }

    /// <summary>
    /// Create a SpecRunner with the specified execution strategy.
    /// </summary>
    /// <param name="middleware">Middleware executed in order (first wraps last)</param>
    /// <param name="configuration">Optional configuration for reporters and formatters</param>
    /// <param name="executionStrategy">Strategy for executing specs</param>
    /// <param name="bail">Stop execution after first failure</param>
    public SpecRunner(
        IEnumerable<ISpecMiddleware> middleware,
        DraftSpecConfiguration? configuration,
        ISpecExecutionStrategy executionStrategy,
        bool bail = false)
    {
        _middleware = middleware.ToList();
        _configuration = configuration;
        _executionStrategy = executionStrategy;
        _bail = bail;
        _reporters = configuration?.Reporters.All.ToList() ?? [];
        _pipeline = BuildPipeline();
    }

    /// <summary>
    /// Create a fluent builder for configuring the runner.
    /// </summary>
    public static SpecRunnerBuilder Create()
    {
        return new SpecRunnerBuilder();
    }

    private Func<SpecExecutionContext, Task<SpecResult>> BuildPipeline()
    {
        // Start with core execution
        var pipeline = ExecuteCoreAsync;

        // Wrap with middleware in reverse order (last added wraps first)
        foreach (var mw in _middleware.Reverse())
        {
            var current = pipeline;
            pipeline = ctx => mw.ExecuteAsync(ctx, current);
        }

        return pipeline;
    }

    /// <summary>
    /// Execute specs synchronously from a Spec entry point.
    /// </summary>
    public IList<SpecResult> Run(Spec spec)
    {
        return Run(spec.RootContext);
    }

    /// <summary>
    /// Execute specs synchronously from a root context.
    /// </summary>
    public IList<SpecResult> Run(SpecContext rootContext)
    {
        return RunAsync(rootContext).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Execute specs asynchronously from a Spec entry point.
    /// </summary>
    public Task<IList<SpecResult>> RunAsync(Spec spec, CancellationToken cancellationToken = default)
    {
        return RunAsync(spec.RootContext, cancellationToken);
    }

    /// <summary>
    /// Execute specs asynchronously from a root context.
    /// Walks the context tree, executing specs through the middleware pipeline.
    /// </summary>
    public async Task<IList<SpecResult>> RunAsync(SpecContext rootContext, CancellationToken cancellationToken = default)
    {
        var results = new List<SpecResult>();

        // Use cached values from SpecContext (computed during tree construction)
        var hasFocused = rootContext.HasFocusedDescendants;

        // Reset bail state for this run
        _bailTriggered = false;

        // Notify reporters that run is starting (use cached spec count)
        var totalSpecs = rootContext.TotalSpecCount;
        var startTime = DateTime.UtcNow;
        await NotifyRunStartingAsync(totalSpecs, startTime).ConfigureAwait(false);

        // Use a mutable list for context path - push/pop as we traverse
        // This avoids ImmutableList allocations on every context entry
        var contextPath = new List<string>();
        await RunContextAsync(rootContext, contextPath, results, hasFocused, cancellationToken).ConfigureAwait(false);

        return results;
    }

    private async Task NotifyRunStartingAsync(int totalSpecs, DateTime startTime)
    {
        if (_reporters.Count == 0) return;

        var startContext = new RunStartingContext(totalSpecs, startTime);
        foreach (var reporter in _reporters) await reporter.OnRunStartingAsync(startContext).ConfigureAwait(false);
    }

    private async Task NotifySpecCompletedAsync(SpecResult result)
    {
        if (_reporters.Count == 0) return;

        if (_reporters.Count == 1)
        {
            // Single reporter - no parallelism overhead
            await _reporters[0].OnSpecCompletedAsync(result).ConfigureAwait(false);
        }
        else
        {
            // Multiple reporters - notify in parallel
            await Task.WhenAll(_reporters.Select(r => r.OnSpecCompletedAsync(result))).ConfigureAwait(false);
        }
    }

    private async Task NotifySpecsBatchCompletedAsync(IReadOnlyList<SpecResult> results)
    {
        if (_reporters.Count == 0 || results.Count == 0) return;

        if (_reporters.Count == 1)
        {
            // Single reporter - call batch method directly
            await _reporters[0].OnSpecsBatchCompletedAsync(results).ConfigureAwait(false);
        }
        else
        {
            // Multiple reporters - notify in parallel
            await Task.WhenAll(_reporters.Select(r => r.OnSpecsBatchCompletedAsync(results))).ConfigureAwait(false);
        }
    }

    private async Task RunContextAsync(
        SpecContext context,
        List<string> contextPath,
        List<SpecResult> results,
        bool hasFocused,
        CancellationToken cancellationToken)
    {
        // Check for cancellation
        cancellationToken.ThrowIfCancellationRequested();

        // If bail was triggered, skip remaining specs in this context
        if (_bailTriggered)
        {
            await SkipRemainingSpecsInContext(context, contextPath, results, hasFocused).ConfigureAwait(false);
            return;
        }

        // Push this context's description onto the path (if non-empty)
        var pushed = false;
        if (!string.IsNullOrEmpty(context.Description))
        {
            contextPath.Add(context.Description);
            pushed = true;
        }

        // Run beforeAll (always sequential)
        if (context.BeforeAll != null)
            await context.BeforeAll().ConfigureAwait(false);

        try
        {
            // Take a snapshot of the context path for this batch of specs
            var pathSnapshot = contextPath.ToArray();

            // Run specs in this context using the configured execution strategy
            var strategyContext = new SpecExecutionStrategyContext
            {
                Specs = context.Specs,
                Context = context,
                ContextPath = pathSnapshot,
                Results = results,
                HasFocused = hasFocused,
                RunSpec = RunSpecAsync,
                NotifyCompleted = NotifySpecCompletedAsync,
                NotifyBatchCompleted = NotifySpecsBatchCompletedAsync,
                IsBailTriggered = () => _bailTriggered,
                SignalBail = () => _bailTriggered = true,
                BailEnabled = _bail
            };
            await _executionStrategy.ExecuteAsync(strategyContext, cancellationToken).ConfigureAwait(false);

            // Recurse into children (always sequential to maintain context isolation)
            foreach (var child in context.Children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_bailTriggered)
                {
                    // Skip remaining children
                    await SkipRemainingSpecsInContext(child, contextPath, results, hasFocused).ConfigureAwait(false);
                }
                else
                {
                    await RunContextAsync(child, contextPath, results, hasFocused, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            // Pop this context's description from the path
            if (pushed)
                contextPath.RemoveAt(contextPath.Count - 1);

            // Run afterAll (always sequential)
            if (context.AfterAll != null)
                await context.AfterAll().ConfigureAwait(false);
        }
    }

    private async Task<SpecResult> RunSpecAsync(
        SpecDefinition spec,
        SpecContext context,
        IReadOnlyList<string> contextPath,
        bool hasFocused)
    {
        // Quick returns for non-executable specs (bypass pipeline)
        if (hasFocused && !spec.IsFocused)
            return new SpecResult(spec, SpecStatus.Skipped, contextPath);

        if (spec.IsSkipped)
            return new SpecResult(spec, SpecStatus.Skipped, contextPath);

        if (spec.IsPending)
            return new SpecResult(spec, SpecStatus.Pending, contextPath);

        // Create execution context and run through pipeline
        var executionContext = new SpecExecutionContext
        {
            Spec = spec,
            Context = context,
            ContextPath = contextPath,
            HasFocused = hasFocused
        };

        return await _pipeline(executionContext).ConfigureAwait(false);
    }

    /// <summary>
    /// Skip all specs in a context and its children (used when bail is triggered).
    /// </summary>
    private async Task SkipRemainingSpecsInContext(
        SpecContext context,
        List<string> contextPath,
        List<SpecResult> results,
        bool hasFocused)
    {
        // Push this context's description onto the path (if non-empty)
        var pushed = false;
        if (!string.IsNullOrEmpty(context.Description))
        {
            contextPath.Add(context.Description);
            pushed = true;
        }

        try
        {
            // Take a snapshot for this context's specs
            var pathSnapshot = contextPath.ToArray();

            // Skip all specs in this context
            foreach (var spec in context.Specs)
            {
                var skippedResult = new SpecResult(spec, SpecStatus.Skipped, pathSnapshot);
                results.Add(skippedResult);
                await NotifySpecCompletedAsync(skippedResult).ConfigureAwait(false);
            }

            // Recursively skip children
            foreach (var child in context.Children)
            {
                await SkipRemainingSpecsInContext(child, contextPath, results, hasFocused).ConfigureAwait(false);
            }
        }
        finally
        {
            // Pop this context's description from the path
            if (pushed)
                contextPath.RemoveAt(contextPath.Count - 1);
        }
    }

    /// <summary>
    /// Core spec execution - runs hooks and spec body.
    /// This is the terminal handler in the middleware pipeline.
    /// </summary>
    private async Task<SpecResult> ExecuteCoreAsync(SpecExecutionContext ctx)
    {
        // Time beforeEach hooks
        var beforeSw = Stopwatch.StartNew();
        await RunBeforeEachHooksAsync(ctx.Context).ConfigureAwait(false);
        beforeSw.Stop();
        var beforeEachDuration = beforeSw.Elapsed;

        // Time spec body
        var specSw = Stopwatch.StartNew();
        SpecStatus status;
        Exception? exception = null;

        // Set snapshot context for toMatchSnapshot() assertions
        SnapshotContext.Current = new SnapshotInfo(
            string.Join(" ", ctx.ContextPath.Append(ctx.Spec.Description)),
            ctx.ContextPath,
            ctx.Spec.Description);

        // Set let scope for let/get fixtures
        LetScope.Current = new LetScope(ctx.Context);

        try
        {
            await ctx.Spec.Body!.Invoke().ConfigureAwait(false);
            specSw.Stop();
            status = SpecStatus.Passed;
        }
        catch (Exception ex)
        {
            specSw.Stop();
            status = SpecStatus.Failed;
            exception = ex;
        }
        finally
        {
            LetScope.Current = null;
            SnapshotContext.Current = null;
        }

        var specDuration = specSw.Elapsed;

        // Time afterEach hooks (always run, even on failure)
        var afterSw = Stopwatch.StartNew();
        await RunAfterEachHooksAsync(ctx.Context).ConfigureAwait(false);
        afterSw.Stop();
        var afterEachDuration = afterSw.Elapsed;

        return new SpecResult(ctx.Spec, status, ctx.ContextPath, specDuration, exception)
        {
            BeforeEachDuration = beforeEachDuration,
            AfterEachDuration = afterEachDuration
        };
    }

    private static async Task RunBeforeEachHooksAsync(SpecContext context)
    {
        // Use cached hook chain for better performance
        foreach (var hook in context.GetBeforeEachChain()) await hook().ConfigureAwait(false);
    }

    private static async Task RunAfterEachHooksAsync(SpecContext context)
    {
        // Use cached hook chain for better performance
        foreach (var hook in context.GetAfterEachChain()) await hook().ConfigureAwait(false);
    }
}
