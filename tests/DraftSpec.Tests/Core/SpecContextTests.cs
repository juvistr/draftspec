namespace DraftSpec.Tests.Core;

/// <summary>
/// Tests for SpecContext, including hook chain optimization.
/// </summary>
public class SpecContextTests
{
    #region Singleton Empty Collection Optimization

    [Test]
    public async Task GetBeforeEachChain_WithoutHooks_ReturnsSingletonEmpty()
    {
        var context1 = new SpecContext("context1");
        var context2 = new SpecContext("context2");

        var chain1 = context1.GetBeforeEachChain();
        var chain2 = context2.GetBeforeEachChain();

        // Both should return the same singleton instance
        await Assert.That(chain1).IsEmpty();
        await Assert.That(chain2).IsEmpty();
        await Assert.That(ReferenceEquals(chain1, chain2)).IsTrue();
    }

    [Test]
    public async Task GetAfterEachChain_WithoutHooks_ReturnsSingletonEmpty()
    {
        var context1 = new SpecContext("context1");
        var context2 = new SpecContext("context2");

        var chain1 = context1.GetAfterEachChain();
        var chain2 = context2.GetAfterEachChain();

        // Both should return the same singleton instance
        await Assert.That(chain1).IsEmpty();
        await Assert.That(chain2).IsEmpty();
        await Assert.That(ReferenceEquals(chain1, chain2)).IsTrue();
    }

    [Test]
    public async Task GetBeforeEachChain_NestedWithoutHooks_ReturnsSingletonEmpty()
    {
        var parent = new SpecContext("parent");
        var child = new SpecContext("child", parent);
        var grandchild = new SpecContext("grandchild", child);

        var parentChain = parent.GetBeforeEachChain();
        var childChain = child.GetBeforeEachChain();
        var grandchildChain = grandchild.GetBeforeEachChain();

        // All should return the same singleton instance
        await Assert.That(parentChain).IsEmpty();
        await Assert.That(ReferenceEquals(parentChain, childChain)).IsTrue();
        await Assert.That(ReferenceEquals(childChain, grandchildChain)).IsTrue();
    }

    [Test]
    public async Task GetAfterEachChain_NestedWithoutHooks_ReturnsSingletonEmpty()
    {
        var parent = new SpecContext("parent");
        var child = new SpecContext("child", parent);
        var grandchild = new SpecContext("grandchild", child);

        var parentChain = parent.GetAfterEachChain();
        var childChain = child.GetAfterEachChain();
        var grandchildChain = grandchild.GetAfterEachChain();

        // All should return the same singleton instance
        await Assert.That(parentChain).IsEmpty();
        await Assert.That(ReferenceEquals(parentChain, childChain)).IsTrue();
        await Assert.That(ReferenceEquals(childChain, grandchildChain)).IsTrue();
    }

    [Test]
    public async Task GetBeforeEachChain_WithHooks_ReturnsNewList()
    {
        var context1 = new SpecContext("context1") { BeforeEach = () => Task.CompletedTask };
        var context2 = new SpecContext("context2") { BeforeEach = () => Task.CompletedTask };

        var chain1 = context1.GetBeforeEachChain();
        var chain2 = context2.GetBeforeEachChain();

        // Should have hooks
        await Assert.That(chain1).Count().IsEqualTo(1);
        await Assert.That(chain2).Count().IsEqualTo(1);
        // Should be different instances
        await Assert.That(ReferenceEquals(chain1, chain2)).IsFalse();
    }

    [Test]
    public async Task GetAfterEachChain_WithHooks_ReturnsNewList()
    {
        var context1 = new SpecContext("context1") { AfterEach = () => Task.CompletedTask };
        var context2 = new SpecContext("context2") { AfterEach = () => Task.CompletedTask };

        var chain1 = context1.GetAfterEachChain();
        var chain2 = context2.GetAfterEachChain();

        // Should have hooks
        await Assert.That(chain1).Count().IsEqualTo(1);
        await Assert.That(chain2).Count().IsEqualTo(1);
        // Should be different instances
        await Assert.That(ReferenceEquals(chain1, chain2)).IsFalse();
    }

    [Test]
    public async Task GetBeforeEachChain_ChildWithoutHooks_ParentWithHook_ReturnsNewList()
    {
        var parent = new SpecContext("parent") { BeforeEach = () => Task.CompletedTask };
        var child = new SpecContext("child", parent);

        var childChain = child.GetBeforeEachChain();

        // Child should inherit parent's hook
        await Assert.That(childChain).Count().IsEqualTo(1);
    }

    [Test]
    public async Task GetAfterEachChain_ChildWithoutHooks_ParentWithHook_ReturnsNewList()
    {
        var parent = new SpecContext("parent") { AfterEach = () => Task.CompletedTask };
        var child = new SpecContext("child", parent);

        var childChain = child.GetAfterEachChain();

        // Child should inherit parent's hook
        await Assert.That(childChain).Count().IsEqualTo(1);
    }

    #endregion

    #region Hook Chain Caching

    [Test]
    public async Task GetBeforeEachChain_CalledMultipleTimes_ReturnsCachedInstance()
    {
        var context = new SpecContext("test") { BeforeEach = () => Task.CompletedTask };

        var chain1 = context.GetBeforeEachChain();
        var chain2 = context.GetBeforeEachChain();

        await Assert.That(ReferenceEquals(chain1, chain2)).IsTrue();
    }

    [Test]
    public async Task GetAfterEachChain_CalledMultipleTimes_ReturnsCachedInstance()
    {
        var context = new SpecContext("test") { AfterEach = () => Task.CompletedTask };

        var chain1 = context.GetAfterEachChain();
        var chain2 = context.GetAfterEachChain();

        await Assert.That(ReferenceEquals(chain1, chain2)).IsTrue();
    }

    #endregion

    #region Hook Chain Order

    [Test]
    public async Task GetBeforeEachChain_ReturnsParentToChildOrder()
    {
        var executionOrder = new List<string>();

        var parent = new SpecContext("parent")
        {
            BeforeEach = () => { executionOrder.Add("parent"); return Task.CompletedTask; }
        };
        var child = new SpecContext("child", parent)
        {
            BeforeEach = () => { executionOrder.Add("child"); return Task.CompletedTask; }
        };

        var chain = child.GetBeforeEachChain();
        foreach (var hook in chain)
        {
            await hook();
        }

        await Assert.That(executionOrder).IsEquivalentTo(["parent", "child"]);
    }

    [Test]
    public async Task GetAfterEachChain_ReturnsChildToParentOrder()
    {
        var executionOrder = new List<string>();

        var parent = new SpecContext("parent")
        {
            AfterEach = () => { executionOrder.Add("parent"); return Task.CompletedTask; }
        };
        var child = new SpecContext("child", parent)
        {
            AfterEach = () => { executionOrder.Add("child"); return Task.CompletedTask; }
        };

        var chain = child.GetAfterEachChain();
        foreach (var hook in chain)
        {
            await hook();
        }

        await Assert.That(executionOrder).IsEquivalentTo(["child", "parent"]);
    }

    #endregion

    #region TotalSpecCount Incremental Computation

    [Test]
    public async Task TotalSpecCount_EmptyContext_IsZero()
    {
        var context = new SpecContext("test");

        await Assert.That(context.TotalSpecCount).IsEqualTo(0);
    }

    [Test]
    public async Task TotalSpecCount_SingleSpec_IsOne()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec", () => { }));

        await Assert.That(context.TotalSpecCount).IsEqualTo(1);
    }

    [Test]
    public async Task TotalSpecCount_MultipleSpecs_CountsAll()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec1", () => { }));
        context.AddSpec(new SpecDefinition("spec2", () => { }));
        context.AddSpec(new SpecDefinition("spec3", () => { }));

        await Assert.That(context.TotalSpecCount).IsEqualTo(3);
    }

    [Test]
    public async Task TotalSpecCount_NestedContexts_PropagatesUp()
    {
        var parent = new SpecContext("parent");
        var child = new SpecContext("child", parent);

        child.AddSpec(new SpecDefinition("child spec", () => { }));

        // Both parent and child should count the spec
        await Assert.That(child.TotalSpecCount).IsEqualTo(1);
        await Assert.That(parent.TotalSpecCount).IsEqualTo(1);
    }

    [Test]
    public async Task TotalSpecCount_DeepNesting_PropagatesAllTheWay()
    {
        var root = new SpecContext("root");
        var level1 = new SpecContext("level1", root);
        var level2 = new SpecContext("level2", level1);
        var level3 = new SpecContext("level3", level2);

        level3.AddSpec(new SpecDefinition("deep spec", () => { }));

        await Assert.That(level3.TotalSpecCount).IsEqualTo(1);
        await Assert.That(level2.TotalSpecCount).IsEqualTo(1);
        await Assert.That(level1.TotalSpecCount).IsEqualTo(1);
        await Assert.That(root.TotalSpecCount).IsEqualTo(1);
    }

    [Test]
    public async Task TotalSpecCount_MultipleBranches_SumsCorrectly()
    {
        var root = new SpecContext("root");
        var child1 = new SpecContext("child1", root);
        var child2 = new SpecContext("child2", root);

        root.AddSpec(new SpecDefinition("root spec", () => { }));
        child1.AddSpec(new SpecDefinition("child1 spec 1", () => { }));
        child1.AddSpec(new SpecDefinition("child1 spec 2", () => { }));
        child2.AddSpec(new SpecDefinition("child2 spec", () => { }));

        await Assert.That(child1.TotalSpecCount).IsEqualTo(2);
        await Assert.That(child2.TotalSpecCount).IsEqualTo(1);
        await Assert.That(root.TotalSpecCount).IsEqualTo(4);
    }

    #endregion

    #region HasFocusedDescendants Incremental Computation

    [Test]
    public async Task HasFocusedDescendants_EmptyContext_IsFalse()
    {
        var context = new SpecContext("test");

        await Assert.That(context.HasFocusedDescendants).IsFalse();
    }

    [Test]
    public async Task HasFocusedDescendants_NonFocusedSpec_IsFalse()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec", () => { }));

        await Assert.That(context.HasFocusedDescendants).IsFalse();
    }

    [Test]
    public async Task HasFocusedDescendants_FocusedSpec_IsTrue()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("spec", () => { }) { IsFocused = true });

        await Assert.That(context.HasFocusedDescendants).IsTrue();
    }

    [Test]
    public async Task HasFocusedDescendants_NestedFocusedSpec_PropagatesUp()
    {
        var parent = new SpecContext("parent");
        var child = new SpecContext("child", parent);

        child.AddSpec(new SpecDefinition("focused spec", () => { }) { IsFocused = true });

        await Assert.That(child.HasFocusedDescendants).IsTrue();
        await Assert.That(parent.HasFocusedDescendants).IsTrue();
    }

    [Test]
    public async Task HasFocusedDescendants_DeepNesting_PropagatesAllTheWay()
    {
        var root = new SpecContext("root");
        var level1 = new SpecContext("level1", root);
        var level2 = new SpecContext("level2", level1);
        var level3 = new SpecContext("level3", level2);

        level3.AddSpec(new SpecDefinition("deep focused", () => { }) { IsFocused = true });

        await Assert.That(level3.HasFocusedDescendants).IsTrue();
        await Assert.That(level2.HasFocusedDescendants).IsTrue();
        await Assert.That(level1.HasFocusedDescendants).IsTrue();
        await Assert.That(root.HasFocusedDescendants).IsTrue();
    }

    [Test]
    public async Task HasFocusedDescendants_OnlyInOneBranch_DoesNotAffectSibling()
    {
        var root = new SpecContext("root");
        var child1 = new SpecContext("child1", root);
        var child2 = new SpecContext("child2", root);

        child1.AddSpec(new SpecDefinition("focused", () => { }) { IsFocused = true });
        child2.AddSpec(new SpecDefinition("not focused", () => { }));

        await Assert.That(child1.HasFocusedDescendants).IsTrue();
        await Assert.That(child2.HasFocusedDescendants).IsFalse();
        await Assert.That(root.HasFocusedDescendants).IsTrue();
    }

    [Test]
    public async Task HasFocusedDescendants_AlreadyTrue_DoesNotRepropagate()
    {
        var root = new SpecContext("root");
        var child = new SpecContext("child", root);

        // Add first focused spec
        child.AddSpec(new SpecDefinition("focused1", () => { }) { IsFocused = true });

        await Assert.That(root.HasFocusedDescendants).IsTrue();

        // Add second focused spec - should not cause any issues
        child.AddSpec(new SpecDefinition("focused2", () => { }) { IsFocused = true });

        await Assert.That(root.HasFocusedDescendants).IsTrue();
    }

    #endregion
}
