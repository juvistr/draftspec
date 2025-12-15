using System.Diagnostics;
using DraftSpec.Configuration;
using DraftSpec.Middleware;
using DraftSpec.Plugins;

namespace DraftSpec;

/// <summary>
/// Spec runner that walks the tree and executes specs through a middleware pipeline.
/// </summary>
public class SpecRunner : ISpecRunner
{
    private readonly IReadOnlyList<ISpecMiddleware> _middleware;
    private readonly Func<SpecExecutionContext, Task<SpecResult>> _pipeline;
    private readonly DraftSpecConfiguration? _configuration;

    /// <summary>
    /// Create a SpecRunner with no middleware (default behavior).
    /// </summary>
    public SpecRunner() : this([], null)
    {
    }

    /// <summary>
    /// Create a SpecRunner with the specified middleware.
    /// </summary>
    /// <param name="middleware">Middleware executed in order (first wraps last)</param>
    /// <param name="configuration">Optional configuration for reporters and formatters</param>
    public SpecRunner(IEnumerable<ISpecMiddleware> middleware, DraftSpecConfiguration? configuration = null)
    {
        _middleware = middleware.ToList();
        _configuration = configuration;
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

        foreach (var reporter in _configuration.Reporters.All)
        {
            await reporter.OnSpecCompletedAsync(result);
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

        // Run beforeAll
        if (context.BeforeAll != null)
            await context.BeforeAll();

        try
        {
            // Run specs in this context
            foreach (var spec in context.Specs)
            {
                var result = await RunSpecAsync(spec, context, descriptions, hasFocused);
                results.Add(result);
                await NotifySpecCompletedAsync(result);
            }

            // Recurse into children
            foreach (var child in context.Children)
            {
                await RunContextAsync(child, descriptions, results, hasFocused);
            }
        }
        finally
        {
            // Run afterAll
            if (context.AfterAll != null)
                await context.AfterAll();
        }
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
        // Run beforeEach hooks (walk up the tree)
        await RunBeforeEachHooksAsync(ctx.Context);

        var sw = Stopwatch.StartNew();
        try
        {
            await ctx.Spec.Body!.Invoke();
            sw.Stop();
            return new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new SpecResult(ctx.Spec, SpecStatus.Failed, ctx.ContextPath, sw.Elapsed, ex);
        }
        finally
        {
            // Run afterEach hooks (walk up the tree, child to parent)
            await RunAfterEachHooksAsync(ctx.Context);
        }
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
