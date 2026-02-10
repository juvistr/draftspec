using FsCheck;
using FsCheck.Fluent;

namespace DraftSpec.Tests.Properties;

/// <summary>
/// Property-based tests for SpecContext tree structure invariants.
/// These tests verify tree properties that must hold for all inputs.
/// </summary>
public class SpecContextTreePropertyTests
{
    [Test]
    public void ParentChildRelationship_IsConsistent()
    {
        // Property: When a child is created with a parent, the parent contains the child
        Prop.ForAll<int>(depth =>
        {
            var normalizedDepth = Math.Abs(depth % 5) + 1; // 1-5 depth

            var root = new SpecContext("root");
            var current = root;

            for (var i = 0; i < normalizedDepth; i++)
            {
                var child = new SpecContext($"child_{i}", current);
                if (child.Parent != current) return false;
                if (!current.Children.Contains(child)) return false;
                current = child;
            }

            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void TotalSpecCount_AccumulatesCorrectly()
    {
        // Property: TotalSpecCount equals local specs + descendant specs
        Prop.ForAll<int, int, int>((localSpecs, child1Specs, child2Specs) =>
        {
            var l = Math.Abs(localSpecs % 10);
            var c1 = Math.Abs(child1Specs % 10);
            var c2 = Math.Abs(child2Specs % 10);

            var root = new SpecContext("root");
            var child1 = new SpecContext("child1", root);
            var child2 = new SpecContext("child2", root);

            // Add specs to each context
            for (var i = 0; i < l; i++)
                root.AddSpec(new SpecDefinition($"root spec {i}", () => { }));
            for (var i = 0; i < c1; i++)
                child1.AddSpec(new SpecDefinition($"child1 spec {i}", () => { }));
            for (var i = 0; i < c2; i++)
                child2.AddSpec(new SpecDefinition($"child2 spec {i}", () => { }));

            // Root should have all specs counted
            return root.TotalSpecCount == l + c1 + c2 &&
                   child1.TotalSpecCount == c1 &&
                   child2.TotalSpecCount == c2;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void FocusPropagation_PropagatesUpward()
    {
        // Property: A focused spec in a child makes all ancestors have HasFocusedDescendants
        Prop.ForAll<int>(depth =>
        {
            var normalizedDepth = Math.Abs(depth % 5) + 1; // 1-5 depth

            var root = new SpecContext("root");
            var contexts = new List<SpecContext> { root };
            var current = root;

            // Build nested context chain
            for (var i = 0; i < normalizedDepth; i++)
            {
                var child = new SpecContext($"level_{i}", current);
                contexts.Add(child);
                current = child;
            }

            // Before adding focused spec, no context should have HasFocusedDescendants
            if (contexts.Any(c => c.HasFocusedDescendants)) return false;

            // Add a focused spec to the deepest context
            current.AddSpec(new SpecDefinition("focused spec", () => { }) { IsFocused = true });

            // All ancestors (including current) should now have HasFocusedDescendants
            return contexts.All(c => c.HasFocusedDescendants);
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task BeforeEachChain_OrdersParentToChild()
    {
        // Property: BeforeEach hooks run in parent→child order
        var executionOrder = new List<string>();

        var root = new SpecContext("root");
        var child = new SpecContext("child", root);
        var grandchild = new SpecContext("grandchild", child);

        root.AddBeforeEach(() => { executionOrder.Add("root"); return Task.CompletedTask; });
        child.AddBeforeEach(() => { executionOrder.Add("child"); return Task.CompletedTask; });
        grandchild.AddBeforeEach(() => { executionOrder.Add("grandchild"); return Task.CompletedTask; });

        // Get the chain and execute
        var chain = grandchild.GetBeforeEachChain();
        foreach (var hook in chain)
            await hook();

        await Assert.That(executionOrder.Count).IsEqualTo(3);
        await Assert.That(executionOrder[0]).IsEqualTo("root");
        await Assert.That(executionOrder[1]).IsEqualTo("child");
        await Assert.That(executionOrder[2]).IsEqualTo("grandchild");
    }

    [Test]
    public async Task AfterEachChain_OrdersChildToParent()
    {
        // Property: AfterEach hooks run in child→parent order
        var executionOrder = new List<string>();

        var root = new SpecContext("root");
        var child = new SpecContext("child", root);
        var grandchild = new SpecContext("grandchild", child);

        root.AddAfterEach(() => { executionOrder.Add("root"); return Task.CompletedTask; });
        child.AddAfterEach(() => { executionOrder.Add("child"); return Task.CompletedTask; });
        grandchild.AddAfterEach(() => { executionOrder.Add("grandchild"); return Task.CompletedTask; });

        // Get the chain and execute
        var chain = grandchild.GetAfterEachChain();
        foreach (var hook in chain)
            await hook();

        await Assert.That(executionOrder.Count).IsEqualTo(3);
        await Assert.That(executionOrder[0]).IsEqualTo("grandchild");
        await Assert.That(executionOrder[1]).IsEqualTo("child");
        await Assert.That(executionOrder[2]).IsEqualTo("root");
    }

    [Test]
    public void HookChainLength_EqualsDepthWithHooks()
    {
        // Property: Hook chain length equals number of contexts with hooks in ancestry
        Prop.ForAll<int>(depth =>
        {
            var normalizedDepth = Math.Abs(depth % 5) + 1; // 1-5 depth

            var root = new SpecContext("root");
            root.AddBeforeEach(() => Task.CompletedTask);

            var current = root;
            for (var i = 0; i < normalizedDepth; i++)
            {
                var child = new SpecContext($"level_{i}", current);
                child.AddBeforeEach(() => Task.CompletedTask);
                current = child;
            }

            // Chain length should equal depth + 1 (all contexts have hooks)
            var chain = current.GetBeforeEachChain();
            return chain.Count == normalizedDepth + 1;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task EmptyHookChain_WhenNoHooksInAncestry()
    {
        // Property: Contexts without hooks return empty chain
        var root = new SpecContext("root");
        var child = new SpecContext("child", root);
        var grandchild = new SpecContext("grandchild", child);

        // No hooks set anywhere
        var beforeChain = grandchild.GetBeforeEachChain();
        var afterChain = grandchild.GetAfterEachChain();

        await Assert.That(beforeChain.Count).IsEqualTo(0);
        await Assert.That(afterChain.Count).IsEqualTo(0);
    }

    [Test]
    public async Task HookChain_IsCachedAndIdempotent()
    {
        // Property: Multiple calls to GetBeforeEachChain return same reference
        var root = new SpecContext("root");
        root.AddBeforeEach(() => Task.CompletedTask);

        var child = new SpecContext("child", root);
        child.AddBeforeEach(() => Task.CompletedTask);

        var chain1 = child.GetBeforeEachChain();
        var chain2 = child.GetBeforeEachChain();

        await Assert.That(ReferenceEquals(chain1, chain2)).IsTrue();
    }

    [Test]
    public void Description_CannotBeEmpty()
    {
        // Property: Empty descriptions throw ArgumentException
        var invalidDescriptions = new[] { "", " ", "\t", "\n", "   " };

        foreach (var desc in invalidDescriptions)
        {
            try
            {
                _ = new SpecContext(desc);
                throw new Exception($"Expected ArgumentException for description: '{desc}'");
            }
            catch (ArgumentException)
            {
                // Expected
            }
        }
    }

    [Test]
    public void SpecsCollection_IsReadOnly()
    {
        // Property: Specs collection reflects added specs in order
        Prop.ForAll<int>(count =>
        {
            var normalizedCount = Math.Abs(count % 10) + 1;

            var context = new SpecContext("test");
            var addedSpecs = new List<SpecDefinition>();

            for (var i = 0; i < normalizedCount; i++)
            {
                var spec = new SpecDefinition($"spec {i}", () => { });
                addedSpecs.Add(spec);
                context.AddSpec(spec);
            }

            // Verify order is preserved
            for (var i = 0; i < normalizedCount; i++)
            {
                if (!ReferenceEquals(context.Specs[i], addedSpecs[i]))
                    return false;
            }

            return context.Specs.Count == normalizedCount;
        }).QuickCheckThrowOnFailure();
    }
}
