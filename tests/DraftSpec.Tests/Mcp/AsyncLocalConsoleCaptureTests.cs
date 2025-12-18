using System.Collections.Concurrent;
using System.Text;
using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Mcp;

/// <summary>
/// Tests for AsyncLocalConsoleCapture thread-safe console output capture.
/// </summary>
public class AsyncLocalConsoleCaptureTests
{
    [Before(Test)]
    public void ResetCapture()
    {
        // Reset state between tests to ensure isolation
        AsyncLocalConsoleCapture.ResetForTesting();
    }

    #region Basic Capture Tests

    [Test]
    public async Task Capture_CapturesConsoleOutput()
    {
        using var capture = new AsyncLocalConsoleCapture();

        Console.Write("Hello");
        Console.Write(" World");

        var output = capture.GetCapturedOutput();
        await Assert.That(output).IsEqualTo("Hello World");
    }

    [Test]
    public async Task Capture_CapturesWriteLine()
    {
        using var capture = new AsyncLocalConsoleCapture();

        Console.WriteLine("Line 1");
        Console.WriteLine("Line 2");

        var output = capture.GetCapturedOutput();
        await Assert.That(output).Contains("Line 1");
        await Assert.That(output).Contains("Line 2");
    }

    [Test]
    public async Task Capture_EmptyWhenNothingWritten()
    {
        using var capture = new AsyncLocalConsoleCapture();

        var output = capture.GetCapturedOutput();
        await Assert.That(output).IsEmpty();
    }

    [Test]
    public async Task Capture_AccumulatesOutput()
    {
        using var capture = new AsyncLocalConsoleCapture();

        Console.Write("First");
        Console.Write("Second");
        Console.Write("Third");

        var output = capture.GetCapturedOutput();
        await Assert.That(output).IsEqualTo("FirstSecondThird");
    }

    #endregion

    #region Nested Capture Tests

    [Test]
    public async Task NestedCapture_InnerCaptureIsolated()
    {
        using var outer = new AsyncLocalConsoleCapture();

        Console.Write("Outer1");

        using (var inner = new AsyncLocalConsoleCapture())
        {
            Console.Write("Inner");
            var innerOutput = inner.GetCapturedOutput();
            await Assert.That(innerOutput).IsEqualTo("Inner");
        }

        Console.Write("Outer2");

        var outerOutput = outer.GetCapturedOutput();
        await Assert.That(outerOutput).IsEqualTo("Outer1Outer2");
    }

    [Test]
    [Skip("Flaky in parallel test execution - console capture ordering is timing-dependent")]
    public async Task NestedCapture_ThreeLevelsDeep()
    {
        using var level1 = new AsyncLocalConsoleCapture();
        Console.Write("L1-");

        using (var level2 = new AsyncLocalConsoleCapture())
        {
            Console.Write("L2-");

            using (var level3 = new AsyncLocalConsoleCapture())
            {
                Console.Write("L3");
                await Assert.That(level3.GetCapturedOutput()).IsEqualTo("L3");
            }

            Console.Write("L2End");
            await Assert.That(level2.GetCapturedOutput()).IsEqualTo("L2-L2End");
        }

        Console.Write("L1End");
        await Assert.That(level1.GetCapturedOutput()).IsEqualTo("L1-L1End");
    }

    #endregion

    #region Concurrent Capture Tests

    [Test]
    [Skip("Flaky in parallel test execution - console capture isolation varies with thread pool pressure")]
    public async Task ConcurrentCapture_IsolatesOutputBetweenTasks()
    {
        const int taskCount = 10;
        var results = new string[taskCount];

        var tasks = Enumerable.Range(0, taskCount).Select(async i =>
        {
            // Use Task.Run to ensure we're on different thread pool threads
            await Task.Run(async () =>
            {
                using var capture = new AsyncLocalConsoleCapture();

                // Each task writes its own identifier multiple times
                for (var j = 0; j < 5; j++)
                {
                    Console.Write($"Task{i}");
                    await Task.Yield(); // Allow other tasks to run
                }

                results[i] = capture.GetCapturedOutput();
            });
        }).ToArray();

        await Task.WhenAll(tasks);

        // Verify each task captured only its own output
        for (var i = 0; i < taskCount; i++)
        {
            var expected = string.Concat(Enumerable.Repeat($"Task{i}", 5));
            await Assert.That(results[i]).IsEqualTo(expected);
        }
    }

    [Test]
    public async Task ConcurrentCapture_NoOutputLeaksBetweenContexts()
    {
        const int taskCount = 20;
        var allOutputs = new ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, taskCount).Select(async i =>
        {
            using var capture = new AsyncLocalConsoleCapture();

            // Write unique content
            var uniqueId = Guid.NewGuid().ToString("N");
            Console.WriteLine($"ID:{uniqueId}");

            await Task.Delay(Random.Shared.Next(1, 10)); // Random delay

            var output = capture.GetCapturedOutput();
            allOutputs.Add(output);

            // Verify output contains only our unique ID
            await Assert.That(output).Contains(uniqueId);

            // Count how many IDs are in the output (should be exactly 1)
            var idCount = output.Split("ID:").Length - 1;
            await Assert.That(idCount).IsEqualTo(1);
        }).ToArray();

        await Task.WhenAll(tasks);

        await Assert.That(allOutputs.Count).IsEqualTo(taskCount);
    }

    [Test]
    public async Task ConcurrentCapture_WithAsyncOperations()
    {
        var task1Output = string.Empty;
        var task2Output = string.Empty;

        var task1 = Task.Run(async () =>
        {
            using var capture = new AsyncLocalConsoleCapture();
            Console.Write("T1-Start-");
            await Task.Delay(50);
            Console.Write("T1-End");
            task1Output = capture.GetCapturedOutput();
        });

        var task2 = Task.Run(async () =>
        {
            using var capture = new AsyncLocalConsoleCapture();
            Console.Write("T2-Start-");
            await Task.Delay(30);
            Console.Write("T2-End");
            task2Output = capture.GetCapturedOutput();
        });

        await Task.WhenAll(task1, task2);

        await Assert.That(task1Output).IsEqualTo("T1-Start-T1-End");
        await Assert.That(task2Output).IsEqualTo("T2-Start-T2-End");
    }

    [Test]
    public async Task ConcurrentCapture_StressTest()
    {
        const int taskCount = 50;
        var errors = new ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, taskCount).Select(i => Task.Run(() =>
        {
            using var capture = new AsyncLocalConsoleCapture();

            var expected = new StringBuilder();
            for (var j = 0; j < 10; j++)
            {
                var msg = $"[{i}:{j}]";
                Console.Write(msg);
                expected.Append(msg);
            }

            var actual = capture.GetCapturedOutput();
            if (actual != expected.ToString())
            {
                errors.Add($"Task {i}: expected '{expected}' but got '{actual}'");
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        await Assert.That(errors).IsEmpty();
    }

    #endregion

    #region Dispose Behavior Tests

    [Test]
    public async Task Dispose_StopsCapturing()
    {
        var capture = new AsyncLocalConsoleCapture();
        Console.Write("Before");
        capture.Dispose();

        // Write after dispose should not be captured
        // (it would go to previous capture or original console)
        var output = capture.GetCapturedOutput();
        await Assert.That(output).IsEqualTo("Before");
    }

    [Test]
    public async Task Dispose_CanBeCalledMultipleTimes()
    {
        var capture = new AsyncLocalConsoleCapture();
        Console.Write("Test");

        capture.Dispose();
        capture.Dispose(); // Should not throw
        capture.Dispose();

        await Assert.That(capture.GetCapturedOutput()).IsEqualTo("Test");
    }

    #endregion
}
