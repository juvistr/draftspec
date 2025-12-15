using System.Diagnostics;
using DraftSpec.Middleware;

namespace DraftSpec;

/// <summary>
/// Spec runner that walks the tree and executes specs through a middleware pipeline.
/// </summary>
public class SpecRunner : ISpecRunner
{
    private readonly IReadOnlyList<ISpecMiddleware> _middleware;
    private readonly Func<SpecExecutionContext, SpecResult> _pipeline;

    /// <summary>
    /// Create a SpecRunner with no middleware (default behavior).
    /// </summary>
    public SpecRunner() : this([])
    {
    }

    /// <summary>
    /// Create a SpecRunner with the specified middleware.
    /// </summary>
    /// <param name="middleware">Middleware executed in order (first wraps last)</param>
    public SpecRunner(IEnumerable<ISpecMiddleware> middleware)
    {
        _middleware = middleware.ToList();
        _pipeline = BuildPipeline();
    }

    /// <summary>
    /// Create a fluent builder for configuring the runner.
    /// </summary>
    public static SpecRunnerBuilder Create() => new();

    private Func<SpecExecutionContext, SpecResult> BuildPipeline()
    {
        // Start with core execution
        Func<SpecExecutionContext, SpecResult> pipeline = ExecuteCore;

        // Wrap with middleware in reverse order (last added wraps first)
        foreach (var mw in _middleware.Reverse())
        {
            var current = pipeline;
            pipeline = ctx => mw.Execute(ctx, current);
        }

        return pipeline;
    }

    public List<SpecResult> Run(Spec spec) => Run(spec.RootContext);

    public List<SpecResult> Run(SpecContext rootContext)
    {
        var results = new List<SpecResult>();
        var hasFocused = HasFocusedSpecs(rootContext);

        RunContext(rootContext, [], results, hasFocused);

        return results;
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

    private void RunContext(
        SpecContext context,
        List<string> parentDescriptions,
        List<SpecResult> results,
        bool hasFocused)
    {
        var descriptions = parentDescriptions.ToList();
        if (!string.IsNullOrEmpty(context.Description))
            descriptions.Add(context.Description);

        // Run beforeAll
        context.BeforeAll?.Invoke();

        try
        {
            // Run specs in this context
            foreach (var spec in context.Specs)
            {
                var result = RunSpec(spec, context, descriptions, hasFocused);
                results.Add(result);
            }

            // Recurse into children
            foreach (var child in context.Children)
            {
                RunContext(child, descriptions, results, hasFocused);
            }
        }
        finally
        {
            // Run afterAll
            context.AfterAll?.Invoke();
        }
    }

    private SpecResult RunSpec(
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

        return _pipeline(executionContext);
    }

    /// <summary>
    /// Core spec execution - runs hooks and spec body.
    /// This is the terminal handler in the middleware pipeline.
    /// </summary>
    private SpecResult ExecuteCore(SpecExecutionContext ctx)
    {
        // Run beforeEach hooks (walk up the tree)
        RunBeforeEachHooks(ctx.Context);

        var sw = Stopwatch.StartNew();
        try
        {
            ctx.Spec.Body!.Invoke();
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
            RunAfterEachHooks(ctx.Context);
        }
    }

    private static void RunBeforeEachHooks(SpecContext context)
    {
        // Use cached hook chain for better performance
        foreach (var hook in context.GetBeforeEachChain())
        {
            hook.Invoke();
        }
    }

    private static void RunAfterEachHooks(SpecContext context)
    {
        // Use cached hook chain for better performance
        foreach (var hook in context.GetAfterEachChain())
        {
            hook.Invoke();
        }
    }
}
