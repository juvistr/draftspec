using System.Collections.Concurrent;
using DraftSpec.Middleware;

namespace DraftSpec.Tests.Middleware;

/// <summary>
/// Tests for SpecExecutionContext, including lazy Items allocation.
/// </summary>
public class SpecExecutionContextTests
{
    #region Lazy Items Allocation

    [Test]
    public async Task Items_NotAccessedBeforeUse_DoesNotAllocate()
    {
        var context = CreateContext();

        // HasItems should be false without allocating
        await Assert.That(context.HasItems).IsFalse();
    }

    [Test]
    public async Task Items_AccessedButEmpty_HasItemsIsFalse()
    {
        var context = CreateContext();

        // Access Items to trigger allocation
        _ = context.Items;

        // Should be false because empty
        await Assert.That(context.HasItems).IsFalse();
    }

    [Test]
    public async Task Items_WithData_HasItemsIsTrue()
    {
        var context = CreateContext();

        context.Items["key"] = "value";

        await Assert.That(context.HasItems).IsTrue();
    }

    [Test]
    public async Task Items_FirstAccess_AllocatesDictionary()
    {
        var context = CreateContext();

        var items = context.Items;

        await Assert.That(items).IsNotNull();
        await Assert.That(items).IsTypeOf<ConcurrentDictionary<string, object>>();
    }

    [Test]
    public async Task Items_MultipleAccesses_ReturnsSameInstance()
    {
        var context = CreateContext();

        var items1 = context.Items;
        var items2 = context.Items;

        await Assert.That(ReferenceEquals(items1, items2)).IsTrue();
    }

    [Test]
    public async Task Items_ConcurrentAccess_ThreadSafe()
    {
        var context = CreateContext();
        var tasks = new List<Task>();
        var errors = new ConcurrentBag<Exception>();

        // Spawn multiple threads accessing Items concurrently
        for (var i = 0; i < 100; i++)
        {
            // Capture loop variable to avoid closure issues
            var index = i;
            var key = $"key_{index}";
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    context.Items[key] = index;
                    _ = context.Items.TryGetValue(key, out _);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        await Assert.That(errors).IsEmpty();
        await Assert.That(context.Items.Count).IsEqualTo(100);
    }

    #endregion

    #region Items Usage

    [Test]
    public async Task Items_AddAndRetrieve_Works()
    {
        var context = CreateContext();

        context.Items["testKey"] = "testValue";

        await Assert.That(context.Items.TryGetValue("testKey", out var value)).IsTrue();
        await Assert.That(value).IsEqualTo("testValue");
    }

    [Test]
    public async Task Items_MultipleValues_AllStored()
    {
        var context = CreateContext();

        context.Items["key1"] = "value1";
        context.Items["key2"] = 42;
        context.Items["key3"] = new object();

        await Assert.That(context.Items.Count).IsEqualTo(3);
    }

    #endregion

    private static SpecExecutionContext CreateContext()
    {
        var specContext = new SpecContext("test");
        return new SpecExecutionContext
        {
            Spec = new SpecDefinition("test spec", () => { }),
            Context = specContext,
            ContextPath = ["test"],
            HasFocused = false
        };
    }
}
