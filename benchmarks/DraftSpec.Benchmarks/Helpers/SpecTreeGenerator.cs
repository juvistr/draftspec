namespace DraftSpec.Benchmarks.Helpers;

/// <summary>
/// Generates synthetic spec trees for benchmarking purposes.
/// </summary>
public static class SpecTreeGenerator
{
    private static readonly Action NoOpAction = () => { };

    /// <summary>
    /// Creates a flat tree with N async specs, each with a fixed delay.
    /// </summary>
    public static SpecContext CreateAsyncTree(int specCount, int delayMs)
    {
        var root = new SpecContext("Async Root");
        for (var i = 0; i < specCount; i++)
            root.AddSpec(new SpecDefinition(
                $"async spec {i}",
                async () => await Task.Delay(delayMs)));
        return root;
    }

    /// <summary>
    /// Creates a flat tree with N specs in a single context.
    /// </summary>
    public static SpecContext CreateFlatTree(int specCount)
    {
        var root = new SpecContext("Root");
        for (var i = 0; i < specCount; i++) root.AddSpec(new SpecDefinition($"spec {i}", NoOpAction));
        return root;
    }

    /// <summary>
    /// Creates a deeply nested tree: depth levels with 1 spec per level.
    /// </summary>
    public static SpecContext CreateDeepTree(int depth)
    {
        var root = new SpecContext("Level 0");
        var current = root;

        for (var i = 1; i < depth; i++)
        {
            var child = new SpecContext($"Level {i}", current);
            child.AddSpec(new SpecDefinition($"spec at level {i}", NoOpAction));
            current = child;
        }

        // Add a spec at root level too
        root.AddSpec(new SpecDefinition("spec at level 0", NoOpAction));

        return root;
    }

    /// <summary>
    /// Creates a wide tree: N contexts at the same level, each with M specs.
    /// </summary>
    public static SpecContext CreateWideTree(int contextCount, int specsPerContext)
    {
        var root = new SpecContext("Root");

        for (var c = 0; c < contextCount; c++)
        {
            var context = new SpecContext($"Context {c}", root);
            for (var s = 0; s < specsPerContext; s++) context.AddSpec(new SpecDefinition($"spec {s}", NoOpAction));
        }

        return root;
    }

    /// <summary>
    /// Creates a balanced tree with realistic nesting (3 levels, distributed specs).
    /// </summary>
    public static SpecContext CreateBalancedTree(int totalSpecs)
    {
        var root = new SpecContext("Root");

        // Distribute specs across 3 levels
        // Level 1: 20% of specs directly under root
        // Level 2: 50% of specs in child contexts
        // Level 3: 30% of specs in grandchild contexts

        var level1Count = Math.Max(1, totalSpecs / 5);
        var level2Count = Math.Max(1, totalSpecs / 2);
        var level3Count = totalSpecs - level1Count - level2Count;

        // Level 1 specs (directly under root)
        for (var i = 0; i < level1Count; i++) root.AddSpec(new SpecDefinition($"root spec {i}", NoOpAction));

        // Distribute level 2 and 3 specs across ~5 child contexts
        var childContexts = Math.Min(5, Math.Max(1, totalSpecs / 20));
        var specsPerLevel2Context = level2Count / childContexts;
        var specsPerLevel3Context = level3Count / childContexts;

        for (var c = 0; c < childContexts; c++)
        {
            var child = new SpecContext($"Context {c}", root);

            // Level 2 specs
            for (var s = 0; s < specsPerLevel2Context; s++)
                child.AddSpec(new SpecDefinition($"child {c} spec {s}", NoOpAction));

            // Create 2 grandchild contexts per child
            var grandchildContexts = Math.Min(2, Math.Max(1, specsPerLevel3Context / 5));
            var specsPerGrandchild = specsPerLevel3Context / grandchildContexts;

            for (var g = 0; g < grandchildContexts; g++)
            {
                var grandchild = new SpecContext($"Context {c}.{g}", child);
                for (var s = 0; s < specsPerGrandchild; s++)
                    grandchild.AddSpec(new SpecDefinition($"grandchild {c}.{g} spec {s}", NoOpAction));
            }
        }

        return root;
    }
}