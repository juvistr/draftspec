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
        await Assert.That(chain1).HasCount().EqualTo(1);
        await Assert.That(chain2).HasCount().EqualTo(1);
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
        await Assert.That(chain1).HasCount().EqualTo(1);
        await Assert.That(chain2).HasCount().EqualTo(1);
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
        await Assert.That(childChain).HasCount().EqualTo(1);
    }

    [Test]
    public async Task GetAfterEachChain_ChildWithoutHooks_ParentWithHook_ReturnsNewList()
    {
        var parent = new SpecContext("parent") { AfterEach = () => Task.CompletedTask };
        var child = new SpecContext("child", parent);

        var childChain = child.GetAfterEachChain();

        // Child should inherit parent's hook
        await Assert.That(childChain).HasCount().EqualTo(1);
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
}
