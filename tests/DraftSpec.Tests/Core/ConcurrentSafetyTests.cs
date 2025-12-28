namespace DraftSpec.Tests.Core;

/// <summary>
/// Tests for thread safety and concurrent modification scenarios.
/// These tests verify that the framework behaves correctly under concurrent access
/// during parallel spec execution. Note: Tree construction (AddSpec, AddChild) is
/// synchronous and not designed for concurrent modification.
/// </summary>
public class ConcurrentSafetyTests
{

    #region Hook Chain Caching Safety

    [Test]
    public async Task SpecContext_GetBeforeEachChain_SafeDuringConcurrentAccess()
    {
        // Arrange
        var parent = new SpecContext("parent");
        parent.BeforeEach = () => Task.CompletedTask;

        var child = new SpecContext("child", parent);
        child.BeforeEach = () => Task.CompletedTask;

        var exceptions = new List<Exception>();
        var results = new List<IReadOnlyList<Func<Task>>>();
        var tasks = new List<Task>();

        // Act - Get hook chain from multiple threads concurrently
        for (var i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var chain = child.GetBeforeEachChain();
                    lock (results) results.Add(chain);
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All chains should be retrieved without exceptions
        await Assert.That(exceptions).IsEmpty();
        await Assert.That(results).HasCount().EqualTo(100);

        // All results should have the same content (cached chain)
        foreach (var chain in results)
        {
            await Assert.That(chain).HasCount().EqualTo(2);
        }
    }

    [Test]
    public async Task SpecContext_GetAfterEachChain_SafeDuringConcurrentAccess()
    {
        // Arrange
        var parent = new SpecContext("parent");
        parent.AfterEach = () => Task.CompletedTask;

        var child = new SpecContext("child", parent);
        child.AfterEach = () => Task.CompletedTask;

        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        // Act - Get hook chain from multiple threads concurrently
        for (var i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    _ = child.GetAfterEachChain();
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        await Assert.That(exceptions).IsEmpty();
    }

    #endregion

    #region Runner Execution Safety

    [Test]
    public async Task SpecRunner_ParallelExecution_IsolatesSpecState()
    {
        // Arrange
        var context = new SpecContext("parallel");
        var sharedCounter = 0;
        var lockObj = new object();
        var specResults = new int[10];

        // Add specs that increment a counter
        for (var i = 0; i < 10; i++)
        {
            var index = i;
            context.AddSpec(new SpecDefinition($"spec-{i}", () =>
            {
                // Simulate some work
                Thread.Sleep(10);
                lock (lockObj)
                {
                    specResults[index] = ++sharedCounter;
                }
            }));
        }

        // Act - Run with parallel execution
        var runner = new SpecRunnerBuilder()
            .WithParallelExecution(4)
            .Build();
        var results = await runner.RunAsync(context);

        // Assert - All specs should have executed
        await Assert.That(results).HasCount().EqualTo(10);
        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();
        await Assert.That(sharedCounter).IsEqualTo(10);
    }

    [Test]
    public async Task SpecRunner_ParallelExecution_HandlesExceptionsInParallel()
    {
        // Arrange
        var context = new SpecContext("parallel");

        for (var i = 0; i < 10; i++)
        {
            var index = i;
            if (index % 2 == 0)
            {
                context.AddSpec(new SpecDefinition($"failing-{i}", () =>
                {
                    Thread.Sleep(5);
                    throw new Exception($"Spec {index} failed");
                }));
            }
            else
            {
                context.AddSpec(new SpecDefinition($"passing-{i}", () =>
                {
                    Thread.Sleep(5);
                }));
            }
        }

        // Act
        var runner = new SpecRunnerBuilder()
            .WithParallelExecution(4)
            .Build();
        var results = await runner.RunAsync(context);

        // Assert - All specs should complete (some passed, some failed)
        await Assert.That(results).HasCount().EqualTo(10);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(5);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Failed)).IsEqualTo(5);
    }

    [Test]
    public async Task SpecRunner_ResultCollection_ThreadSafe()
    {
        // Arrange
        var context = new SpecContext("results");

        // Add many specs to increase chance of race condition
        for (var i = 0; i < 100; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () => { }));
        }

        // Act
        var runner = new SpecRunnerBuilder()
            .WithParallelExecution(8)
            .Build();
        var results = await runner.RunAsync(context);

        // Assert - All results should be collected
        await Assert.That(results).HasCount().EqualTo(100);
        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();
    }

    #endregion

    #region Bail and Cancellation Safety

    [Test]
    public async Task SpecRunner_BailWithParallel_SafelyCancelsRemaining()
    {
        // Arrange
        var context = new SpecContext("bail");
        var startedCount = 0;

        // First spec fails immediately
        context.AddSpec(new SpecDefinition("failing", () =>
        {
            Interlocked.Increment(ref startedCount);
            throw new Exception("fail");
        }));

        // Add many more specs
        for (var i = 0; i < 20; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () =>
            {
                Interlocked.Increment(ref startedCount);
                Thread.Sleep(50);
            }));
        }

        // Act
        var runner = new SpecRunnerBuilder()
            .WithParallelExecution(4)
            .WithBail()
            .Build();
        var results = await runner.RunAsync(context);

        // Assert - Should have 1 failed and rest skipped (some may have started)
        await Assert.That(results).HasCount().EqualTo(21);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Failed)).IsEqualTo(1);
        // Most should be skipped (bail should stop most from starting)
        await Assert.That(results.Count(r => r.Status == SpecStatus.Skipped)).IsGreaterThan(0);
    }

    #endregion

    #region Focus and Skip Safety

    [Test]
    public async Task SpecRunner_FocusWithParallel_CorrectlyFilters()
    {
        // Arrange
        var context = new SpecContext("focus");

        // Add mix of focused and non-focused specs
        for (var i = 0; i < 10; i++)
        {
            context.AddSpec(new SpecDefinition($"normal-{i}", () => { }));
        }
        for (var i = 0; i < 5; i++)
        {
            context.AddSpec(new SpecDefinition($"focused-{i}", () => { }) { IsFocused = true });
        }

        // Act
        var runner = new SpecRunnerBuilder()
            .WithParallelExecution(4)
            .Build();
        var results = await runner.RunAsync(context);

        // Assert - Only focused specs should run
        await Assert.That(results).HasCount().EqualTo(15);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(5);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Skipped)).IsEqualTo(10);
    }

    #endregion
}
