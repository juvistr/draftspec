using System.Diagnostics;

namespace DraftSpec;

/// <summary>
/// Simple spec runner that walks the tree and executes specs.
/// </summary>
public class SpecRunner
{
    public List<SpecResult> Run(Spec spec)
    {
        var results = new List<SpecResult>();
        var hasFocused = HasFocusedSpecs(spec.RootContext);

        RunContext(spec.RootContext, [], results, hasFocused);

        return results;
    }

    private static bool HasFocusedSpecs(SpecContext context)
    {
        if (context.Specs.Any(s => s.IsFocused))
            return true;

        return context.Children.Any(HasFocusedSpecs);
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
        var contexts = new Stack<SpecContext>();
        var current = context;
        while (current != null)
        {
            contexts.Push(current);
            current = current.Parent;
        }

        // Run parent to child
        while (contexts.Count > 0)
        {
            contexts.Pop().BeforeEach?.Invoke();
        }
    }

    private static void RunAfterEachHooks(SpecContext context)
    {
        // Run child to parent
        var current = context;
        while (current != null)
        {
            current.AfterEach?.Invoke();
            current = current.Parent;
        }
    }
}
