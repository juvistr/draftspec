using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace DraftSpec.Tests.EdgeCases;

/// <summary>
/// Large-scale and edge case tests for scenarios that stress the framework's limits.
/// Covers concurrent execution at scale, Unicode edge cases, and thread safety.
/// </summary>
public class LargeScaleEdgeCaseTests
{
    #region Large Scale Parallel Execution

    [Test]
    public async Task ParallelExecution_100ConcurrentSpecs_CompletesSuccessfully()
    {
        var context = new SpecContext("concurrent");
        var executedCount = 0;

        for (var i = 0; i < 100; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", async () =>
            {
                Interlocked.Increment(ref executedCount);
                await Task.Delay(10); // Simulate work
            }));
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(20) // High concurrency
            .Build();

        var sw = Stopwatch.StartNew();
        var results = await runner.RunAsync(context);
        sw.Stop();

        await Assert.That(results).Count().IsEqualTo(100);
        await Assert.That(executedCount).IsEqualTo(100);
        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();

        // With 20 parallel threads and 10ms delay, should complete much faster than sequential
        // Sequential would be ~1000ms, parallel should be ~50-100ms
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(500);
    }

    [Test]
    public async Task ParallelExecution_SharedStateRaceCondition_DetectsWithoutCrash()
    {
        // This test verifies the framework handles specs with shared state safely
        // The framework itself shouldn't crash even if user code has race conditions
        var context = new SpecContext("race");
        var sharedCounter = 0; // Intentionally not thread-safe

        for (var i = 0; i < 50; i++)
        {
            context.AddSpec(new SpecDefinition($"spec-{i}", () =>
            {
                // This is intentionally racy - we're testing framework resilience
                var temp = sharedCounter;
                Thread.SpinWait(100);
                sharedCounter = temp + 1;
            }));
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(10)
            .Build();

        // Should complete without throwing, even with racy user code
        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(50);
        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();

        // Counter will likely be less than 50 due to races, but framework shouldn't crash
        await Assert.That(sharedCounter).IsGreaterThan(0);
    }

    [Test]
    public async Task ParallelExecution_ThreadSafeStatisticsTracking()
    {
        var context = new SpecContext("stats");
        var passCount = 0;
        var failCount = 0;

        for (var i = 0; i < 100; i++)
        {
            if (i % 10 == 0)
            {
                context.AddSpec(new SpecDefinition($"fail-{i}", () =>
                {
                    Interlocked.Increment(ref failCount);
                    throw new Exception("intentional");
                }));
            }
            else
            {
                context.AddSpec(new SpecDefinition($"pass-{i}", () =>
                {
                    Interlocked.Increment(ref passCount);
                }));
            }
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(10)
            .Build();

        var results = await runner.RunAsync(context);

        // Verify statistics are accurate under concurrent execution
        await Assert.That(passCount).IsEqualTo(90);
        await Assert.That(failCount).IsEqualTo(10);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(90);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Failed)).IsEqualTo(10);
    }

    [Test]
    public async Task ParallelExecution_HookOrderMaintainedPerSpec()
    {
        var context = new SpecContext("hooks");
        var hookLog = new ConcurrentDictionary<string, List<string>>();

        context.BeforeEach = () =>
        {
            var specId = Environment.CurrentManagedThreadId.ToString();
            hookLog.GetOrAdd(specId, _ => []).Add("before");
            return Task.CompletedTask;
        };

        context.AfterEach = () =>
        {
            var specId = Environment.CurrentManagedThreadId.ToString();
            hookLog.GetOrAdd(specId, _ => []).Add("after");
            return Task.CompletedTask;
        };

        for (var i = 0; i < 20; i++)
        {
            var name = $"spec-{i}";
            context.AddSpec(new SpecDefinition(name, () =>
            {
                var specId = Environment.CurrentManagedThreadId.ToString();
                hookLog.GetOrAdd(specId, _ => []).Add("spec");
            }));
        }

        var runner = SpecRunner.Create()
            .WithParallelExecution(4)
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(results).Count().IsEqualTo(20);
        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();

        // Each thread should have hooks in correct order: before, spec, after repeating
        foreach (var log in hookLog.Values)
        {
            // Check that we don't have "after" before "before" or "spec" in wrong order
            for (var i = 0; i < log.Count - 2; i += 3)
            {
                if (i + 2 < log.Count)
                {
                    await Assert.That(log[i]).IsEqualTo("before");
                    await Assert.That(log[i + 1]).IsEqualTo("spec");
                    await Assert.That(log[i + 2]).IsEqualTo("after");
                }
            }
        }
    }

    #endregion

    #region Unicode and Special Characters

    [Test]
    public async Task Unicode_EmojiInSpecDescription_HandledCorrectly()
    {
        var context = new SpecContext("emoji tests");
        context.AddSpec(new SpecDefinition("should pass with emoji ✅", () => { }));
        context.AddSpec(new SpecDefinition("should fail with emoji ❌", () => throw new Exception("fail")));
        context.AddSpec(new SpecDefinition("pending with emoji ⏳")); // No body

        var runner = new SpecRunner();
        var results = runner.Run(context);

        await Assert.That(results[0].Spec.Description).IsEqualTo("should pass with emoji ✅");
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(results[1].Spec.Description).IsEqualTo("should fail with emoji ❌");
        await Assert.That(results[1].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[2].Spec.Description).IsEqualTo("pending with emoji ⏳");
        await Assert.That(results[2].Status).IsEqualTo(SpecStatus.Pending);
    }

    [Test]
    public async Task Unicode_MultiByteCharactersInDescription_HandledCorrectly()
    {
        var descriptions = new[]
        {
            "日本語テスト",           // Japanese
            "한국어 테스트",          // Korean
            "中文测试",               // Chinese
            "Тест на русском",        // Russian
            "اختبار عربي",            // Arabic
            "हिंदी परीक्षण",            // Hindi
            "θεστ ελληνικά"           // Greek
        };

        var context = new SpecContext("unicode");
        foreach (var desc in descriptions)
            context.AddSpec(new SpecDefinition(desc, () => { }));

        var runner = new SpecRunner();
        var results = runner.Run(context);

        await Assert.That(results).Count().IsEqualTo(descriptions.Length);
        for (var i = 0; i < descriptions.Length; i++)
            await Assert.That(results[i].Spec.Description).IsEqualTo(descriptions[i]);
    }

    [Test]
    public async Task Unicode_SpecialCharactersInContextDescription_HandledCorrectly()
    {
        var root = new SpecContext("root «special» chars");
        var child = new SpecContext("child ™ © ® context", root);
        child.AddSpec(new SpecDefinition("spec with → arrows ← and • bullets", () => { }));

        var runner = new SpecRunner();
        var results = runner.Run(root);

        await Assert.That(results[0].ContextPath[0]).IsEqualTo("root «special» chars");
        await Assert.That(results[0].ContextPath[1]).IsEqualTo("child ™ © ® context");
        await Assert.That(results[0].Spec.Description).IsEqualTo("spec with → arrows ← and • bullets");
    }

    [Test]
    public async Task Unicode_AssertionMessageWithUnicode_PreservedInFailure()
    {
        var context = new SpecContext("unicode assertions");
        context.AddSpec(new SpecDefinition("fails with unicode message", () =>
        {
            throw new Exception("Expected 日本語 but got 中文 — value mismatch ❌");
        }));

        var runner = new SpecRunner();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[0].Exception!.Message)
            .IsEqualTo("Expected 日本語 but got 中文 — value mismatch ❌");
    }

    [Test]
    public async Task Unicode_CombiningCharacters_HandledCorrectly()
    {
        // Combining characters: é can be represented as e + combining acute
        var combinedE = "e\u0301"; // e + combining acute accent
        var precomposedE = "é";    // precomposed form

        var context = new SpecContext($"test {combinedE} combined");
        context.AddSpec(new SpecDefinition($"spec with {precomposedE} precomposed", () => { }));

        var runner = new SpecRunner();
        var results = runner.Run(context);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        // Both forms should be preserved as-is
        await Assert.That(results[0].ContextPath[0]).Contains("combined");
        await Assert.That(results[0].Spec.Description).Contains("precomposed");
    }

    [Test]
    public async Task Unicode_ZeroWidthCharacters_HandledCorrectly()
    {
        // Zero-width joiner and non-joiner
        var zwj = "\u200D";   // zero-width joiner
        var zwnj = "\u200C";  // zero-width non-joiner

        var context = new SpecContext($"context{zwj}with{zwnj}zero-width");
        context.AddSpec(new SpecDefinition("spec", () => { }));

        var runner = new SpecRunner();
        var results = runner.Run(context);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task Unicode_SurrogatePairs_HandledCorrectly()
    {
        // Emoji that require surrogate pairs in UTF-16
        var emoji = "🎉🚀💻🔥"; // Each is a surrogate pair

        var context = new SpecContext($"celebrate {emoji}");
        context.AddSpec(new SpecDefinition($"rocket {emoji} launch", () => { }));

        var runner = new SpecRunner();
        var results = runner.Run(context);

        await Assert.That(results[0].ContextPath[0]).IsEqualTo($"celebrate {emoji}");
        await Assert.That(results[0].Spec.Description).IsEqualTo($"rocket {emoji} launch");
    }

    #endregion

    #region Deep Nesting Edge Cases

    [Test]
    public async Task DeepNesting_20Levels_WithHooksAtEachLevel_ExecutesCorrectly()
    {
        var hookOrder = new List<string>();
        var lockObj = new object();

        var root = new SpecContext("level-0");
        root.BeforeEach = () =>
        {
            lock (lockObj) hookOrder.Add("before-0");
            return Task.CompletedTask;
        };

        var current = root;
        for (var i = 1; i <= 20; i++)
        {
            var level = i;
            current = new SpecContext($"level-{level}", current);
            current.BeforeEach = () =>
            {
                lock (lockObj) hookOrder.Add($"before-{level}");
                return Task.CompletedTask;
            };
        }

        current.AddSpec(new SpecDefinition("deepest spec", () =>
        {
            lock (lockObj) hookOrder.Add("spec");
        }));

        var runner = new SpecRunner();
        var results = runner.Run(root);

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(results[0].ContextPath).Count().IsEqualTo(21); // 0-20

        // Verify hooks ran in order (parent to child)
        await Assert.That(hookOrder).Count().IsEqualTo(22); // 21 befores + 1 spec
        for (var i = 0; i <= 20; i++)
            await Assert.That(hookOrder[i]).IsEqualTo($"before-{i}");
        await Assert.That(hookOrder[21]).IsEqualTo("spec");
    }

    #endregion
}
