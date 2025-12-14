using System.Diagnostics;

namespace DraftSpec;

/// <summary>
/// Simple spec runner that walks the tree and executes specs.
/// </summary>
public class SpecRunner : ISpecRunner
{
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
        // Skip if focused specs exist and this isn't one
        if (hasFocused && !spec.IsFocused)
        {
            return new SpecResult(spec, SpecStatus.Skipped, contextPath);
        }

        if (spec.IsSkipped)
        {
            return new SpecResult(spec, SpecStatus.Skipped, contextPath);
        }

        if (spec.IsPending)
        {
            return new SpecResult(spec, SpecStatus.Pending, contextPath);
        }

        // Run beforeEach hooks (walk up the tree)
        RunBeforeEachHooks(context);

        var sw = Stopwatch.StartNew();
        try
        {
            spec.Body!.Invoke();
            sw.Stop();

            return new SpecResult(spec, SpecStatus.Passed, contextPath, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new SpecResult(spec, SpecStatus.Failed, contextPath, sw.Elapsed, ex);
        }
        finally
        {
            // Run afterEach hooks (walk up the tree, child to parent)
            RunAfterEachHooks(context);
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
