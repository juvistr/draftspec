using System.Diagnostics;
using DraftSpec.Configuration;
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
    private readonly int _maxDegreeOfParallelism;
    private readonly IReadOnlyList<IReporter> _reporters;
    private readonly bool _bail;
    private volatile bool _bailTriggered;

    /// <summary>
    /// Create a SpecRunner with no middleware (default behavior).
    /// </summary>
    public SpecRunner() : this([], null, 0, false)
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
    {
        _middleware = middleware.ToList();
        _configuration = configuration;
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
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
    public List<SpecResult> Run(Spec spec)
    {
        return Run(spec.RootContext);
    }

    /// <summary>
    /// Execute specs synchronously from a root context.
    /// </summary>
    public List<SpecResult> Run(SpecContext rootContext)
    {
        return RunAsync(rootContext).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Execute specs asynchronously from a Spec entry point.
    /// </summary>
    public Task<List<SpecResult>> RunAsync(Spec spec, CancellationToken cancellationToken = default)
    {
        return RunAsync(spec.RootContext, cancellationToken);
    }

    /// <summary>
    /// Execute specs asynchronously from a root context.
    /// Walks the context tree, executing specs through the middleware pipeline.
    /// </summary>
    public async Task<List<SpecResult>> RunAsync(SpecContext rootContext, CancellationToken cancellationToken = default)
    {
        var results = new List<SpecResult>();

        // Use cached values from SpecContext (computed during tree construction)
        var hasFocused = rootContext.HasFocusedDescendants;

        // Reset bail state for this run
        _bailTriggered = false;

        // Notify reporters that run is starting (use cached spec count)
        var totalSpecs = rootContext.TotalSpecCount;
        var startTime = DateTime.UtcNow;
        await NotifyRunStartingAsync(totalSpecs, startTime);

        // Use a mutable list for context path - push/pop as we traverse
        // This avoids ImmutableList allocations on every context entry
        var contextPath = new List<string>();
        await RunContextAsync(rootContext, contextPath, results, hasFocused, cancellationToken);

        return results;
    }

    private async Task NotifyRunStartingAsync(int totalSpecs, DateTime startTime)
    {
        if (_reporters.Count == 0) return;

        var startContext = new RunStartingContext(totalSpecs, startTime);
        foreach (var reporter in _reporters) await reporter.OnRunStartingAsync(startContext);
    }

    private async Task NotifySpecCompletedAsync(SpecResult result)
    {
        if (_reporters.Count == 0) return;

        if (_reporters.Count == 1)
        {
            // Single reporter - no parallelism overhead
            await _reporters[0].OnSpecCompletedAsync(result);
        }
        else
        {
            // Multiple reporters - notify in parallel
            await Task.WhenAll(_reporters.Select(r => r.OnSpecCompletedAsync(result)));
        }
    }

    private async Task NotifySpecsBatchCompletedAsync(IReadOnlyList<SpecResult> results)
    {
        if (_reporters.Count == 0 || results.Count == 0) return;

        if (_reporters.Count == 1)
        {
            // Single reporter - call batch method directly
            await _reporters[0].OnSpecsBatchCompletedAsync(results);
        }
        else
        {
            // Multiple reporters - notify in parallel
            await Task.WhenAll(_reporters.Select(r => r.OnSpecsBatchCompletedAsync(results)));
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
            await SkipRemainingSpecsInContext(context, contextPath, results, hasFocused);
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
            await context.BeforeAll();

        try
        {
            // Run specs in this context (parallel or sequential)
            if (_maxDegreeOfParallelism > 1 && context.Specs.Count > 1)
                await RunSpecsParallelAsync(context, contextPath, results, hasFocused, cancellationToken);
            else
                await RunSpecsSequentialAsync(context, contextPath, results, hasFocused, cancellationToken);

            // Recurse into children (always sequential to maintain context isolation)
            foreach (var child in context.Children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_bailTriggered)
                {
                    // Skip remaining children
                    await SkipRemainingSpecsInContext(child, contextPath, results, hasFocused);
                }
                else
                {
                    await RunContextAsync(child, contextPath, results, hasFocused, cancellationToken);
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
                await context.AfterAll();
        }
    }

    private async Task RunSpecsSequentialAsync(
        SpecContext context,
        List<string> contextPath,
        List<SpecResult> results,
        bool hasFocused,
        CancellationToken cancellationToken)
    {
        // Take a snapshot of the context path for this batch of specs
        // This avoids repeated ToArray() calls for each spec in the same context
        var pathSnapshot = contextPath.ToArray();

        foreach (var spec in context.Specs)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // If bail triggered, skip remaining specs
            if (_bailTriggered)
            {
                var skippedResult = new SpecResult(spec, SpecStatus.Skipped, pathSnapshot);
                results.Add(skippedResult);
                await NotifySpecCompletedAsync(skippedResult);
                continue;
            }

            var result = await RunSpecAsync(spec, context, pathSnapshot, hasFocused);
            results.Add(result);
            await NotifySpecCompletedAsync(result);

            // Check if we should bail
            if (_bail && result.Status == SpecStatus.Failed)
            {
                _bailTriggered = true;
            }
        }
    }

    private async Task RunSpecsParallelAsync(
        SpecContext context,
        List<string> contextPath,
        List<SpecResult> results,
        bool hasFocused,
        CancellationToken cancellationToken)
    {
        // Take a snapshot of the context path for this batch of specs
        var pathSnapshot = contextPath.ToArray();

        // Create indexed list to preserve order
        var indexedSpecs = context.Specs.Select((spec, index) => (spec, index)).ToList();
        var resultArray = new SpecResult[indexedSpecs.Count];
        var processedFlags = new bool[indexedSpecs.Count];

        // Link external cancellation token with bail cancellation
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism,
            CancellationToken = cts.Token
        };

        try
        {
            await Parallel.ForEachAsync(indexedSpecs, options, async (item, ct) =>
            {
                var (spec, index) = item;

                // Check if bail was triggered before starting
                if (_bailTriggered)
                {
                    resultArray[index] = new SpecResult(spec, SpecStatus.Skipped, pathSnapshot);
                    processedFlags[index] = true;
                    return;
                }

                var result = await RunSpecAsync(spec, context, pathSnapshot, hasFocused);
                resultArray[index] = result;
                processedFlags[index] = true;

                // Check if we should bail
                if (_bail && result.Status == SpecStatus.Failed)
                {
                    _bailTriggered = true;
                    cts.Cancel();
                }
            });
        }
        catch (OperationCanceledException) when (_bailTriggered || cancellationToken.IsCancellationRequested)
        {
            // Expected when bail is triggered or external cancellation requested
        }

        // Rethrow if this was external cancellation (not bail)
        cancellationToken.ThrowIfCancellationRequested();

        // Fill in any specs that weren't processed due to cancellation
        for (var i = 0; i < resultArray.Length; i++)
        {
            if (!processedFlags[i])
            {
                resultArray[i] = new SpecResult(indexedSpecs[i].spec, SpecStatus.Skipped, pathSnapshot);
            }
        }

        // Add results in original order
        results.AddRange(resultArray);

        // Notify reporters in batch (parallel notification to multiple reporters)
        await NotifySpecsBatchCompletedAsync(resultArray);
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

        return await _pipeline(executionContext);
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
                await NotifySpecCompletedAsync(skippedResult);
            }

            // Recursively skip children
            foreach (var child in context.Children)
            {
                await SkipRemainingSpecsInContext(child, contextPath, results, hasFocused);
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
        await RunBeforeEachHooksAsync(ctx.Context);
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

        try
        {
            await ctx.Spec.Body!.Invoke();
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
            SnapshotContext.Current = null;
        }

        var specDuration = specSw.Elapsed;

        // Time afterEach hooks (always run, even on failure)
        var afterSw = Stopwatch.StartNew();
        await RunAfterEachHooksAsync(ctx.Context);
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
        foreach (var hook in context.GetBeforeEachChain()) await hook();
    }

    private static async Task RunAfterEachHooksAsync(SpecContext context)
    {
        // Use cached hook chain for better performance
        foreach (var hook in context.GetAfterEachChain()) await hook();
    }
}
