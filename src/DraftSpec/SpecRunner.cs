using System.Diagnostics;
using DraftSpec.Configuration;
using DraftSpec.Middleware;
using DraftSpec.Plugins;

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

    /// <summary>
    /// Create a SpecRunner with no middleware (default behavior).
    /// </summary>
    public SpecRunner() : this([], null, 0)
    {
    }

    /// <summary>
    /// Create a SpecRunner with the specified middleware.
    /// </summary>
    /// <param name="middleware">Middleware executed in order (first wraps last)</param>
    /// <param name="configuration">Optional configuration for reporters and formatters</param>
    /// <param name="maxDegreeOfParallelism">Maximum parallel spec execution (0 = sequential)</param>
    public SpecRunner(
        IEnumerable<ISpecMiddleware> middleware,
        DraftSpecConfiguration? configuration = null,
        int maxDegreeOfParallelism = 0)
    {
        _middleware = middleware.ToList();
        _configuration = configuration;
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        _pipeline = BuildPipeline();
    }

    /// <summary>
    /// Create a fluent builder for configuring the runner.
    /// </summary>
    public static SpecRunnerBuilder Create() => new();

    private Func<SpecExecutionContext, Task<SpecResult>> BuildPipeline()
    {
        // Start with core execution
        Func<SpecExecutionContext, Task<SpecResult>> pipeline = ExecuteCoreAsync;

        // Wrap with middleware in reverse order (last added wraps first)
        foreach (var mw in _middleware.Reverse())
        {
            var current = pipeline;
            pipeline = ctx => mw.ExecuteAsync(ctx, current);
        }

        return pipeline;
    }

    public List<SpecResult> Run(Spec spec) => Run(spec.RootContext);

    public List<SpecResult> Run(SpecContext rootContext)
    {
        return RunAsync(rootContext).GetAwaiter().GetResult();
    }

    public Task<List<SpecResult>> RunAsync(Spec spec) => RunAsync(spec.RootContext);

    public async Task<List<SpecResult>> RunAsync(SpecContext rootContext)
    {
        var results = new List<SpecResult>();
        var hasFocused = HasFocusedSpecs(rootContext);

        // Notify reporters that run is starting
        var totalSpecs = CountSpecs(rootContext);
        var startTime = DateTime.UtcNow;
        await NotifyRunStartingAsync(totalSpecs, startTime);

        await RunContextAsync(rootContext, [], results, hasFocused);

        return results;
    }

    private static int CountSpecs(SpecContext context)
    {
        return context.Specs.Count + context.Children.Sum(CountSpecs);
    }

    private async Task NotifyRunStartingAsync(int totalSpecs, DateTime startTime)
    {
        if (_configuration == null) return;

        var startContext = new RunStartingContext(totalSpecs, startTime);
        foreach (var reporter in _configuration.Reporters.All)
        {
            await reporter.OnRunStartingAsync(startContext);
        }
    }

    private async Task NotifySpecCompletedAsync(SpecResult result)
    {
        if (_configuration == null) return;

        var reporters = _configuration.Reporters.All.ToList();
        if (reporters.Count <= 1)
        {
            // Single reporter - no parallelism overhead
            foreach (var reporter in reporters)
            {
                await reporter.OnSpecCompletedAsync(result);
            }
        }
        else
        {
            // Multiple reporters - notify in parallel
            await Task.WhenAll(reporters.Select(r => r.OnSpecCompletedAsync(result)));
        }
    }

    private async Task NotifySpecsBatchCompletedAsync(IReadOnlyList<SpecResult> results)
    {
        if (_configuration == null || results.Count == 0) return;

        var reporters = _configuration.Reporters.All.ToList();
        if (reporters.Count <= 1)
        {
            // Single reporter - call batch method directly
            foreach (var reporter in reporters)
            {
                await reporter.OnSpecsBatchCompletedAsync(results);
            }
        }
        else
        {
            // Multiple reporters - notify in parallel
            await Task.WhenAll(reporters.Select(r => r.OnSpecsBatchCompletedAsync(results)));
        }
    }

    private static bool HasFocusedSpecs(SpecContext context)
    {
        if (context.Specs.Any(s => s.IsFocused))
            return true;

        // Explicit loop with early exit for better performance
        foreach (var child in context.Children)
        {
            if (HasFocusedSpecs(child))
                return true;
        }

        return false;
    }

    private async Task RunContextAsync(
        SpecContext context,
        List<string> parentDescriptions,
        List<SpecResult> results,
        bool hasFocused)
    {
        var descriptions = parentDescriptions.ToList();
        if (!string.IsNullOrEmpty(context.Description))
            descriptions.Add(context.Description);

        // Run beforeAll (always sequential)
        if (context.BeforeAll != null)
            await context.BeforeAll();

        try
        {
            // Run specs in this context (parallel or sequential)
            if (_maxDegreeOfParallelism > 1 && context.Specs.Count > 1)
            {
                await RunSpecsParallelAsync(context, descriptions, results, hasFocused);
            }
            else
            {
                await RunSpecsSequentialAsync(context, descriptions, results, hasFocused);
            }

            // Recurse into children (always sequential to maintain context isolation)
            foreach (var child in context.Children)
            {
                await RunContextAsync(child, descriptions, results, hasFocused);
            }
        }
        finally
        {
            // Run afterAll (always sequential)
            if (context.AfterAll != null)
                await context.AfterAll();
        }
    }

    private async Task RunSpecsSequentialAsync(
        SpecContext context,
        List<string> descriptions,
        List<SpecResult> results,
        bool hasFocused)
    {
        foreach (var spec in context.Specs)
        {
            var result = await RunSpecAsync(spec, context, descriptions, hasFocused);
            results.Add(result);
            await NotifySpecCompletedAsync(result);
        }
    }

    private async Task RunSpecsParallelAsync(
        SpecContext context,
        List<string> descriptions,
        List<SpecResult> results,
        bool hasFocused)
    {
        // Create indexed list to preserve order
        var indexedSpecs = context.Specs.Select((spec, index) => (spec, index)).ToList();
        var resultArray = new SpecResult[indexedSpecs.Count];

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism
        };

        await Parallel.ForEachAsync(indexedSpecs, options, async (item, _) =>
        {
            var (spec, index) = item;
            var result = await RunSpecAsync(spec, context, descriptions, hasFocused);
            resultArray[index] = result;
        });

        // Add results in original order
        results.AddRange(resultArray);

        // Notify reporters in batch (parallel notification to multiple reporters)
        await NotifySpecsBatchCompletedAsync(resultArray);
    }

    private async Task<SpecResult> RunSpecAsync(
        SpecDefinition spec,
        SpecContext context,
        List<string> contextPath,
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
        foreach (var hook in context.GetBeforeEachChain())
        {
            await hook();
        }
    }

    private static async Task RunAfterEachHooksAsync(SpecContext context)
    {
        // Use cached hook chain for better performance
        foreach (var hook in context.GetAfterEachChain())
        {
            await hook();
        }
    }
}
