using DraftSpec.Formatters;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.TestingPlatform;

/// <summary>
/// Tests for ResultCaptureReporter.
/// </summary>
public class ResultCaptureReporterTests
{
    #region Properties

    [Test]
    public async Task Name_ReturnsExpectedValue()
    {
        var reporter = new ResultCaptureReporter();

        await Assert.That(reporter.Name).IsEqualTo("MtpResultCapture");
    }

    [Test]
    public async Task Results_InitiallyEmpty()
    {
        var reporter = new ResultCaptureReporter();

        await Assert.That(reporter.Results).IsEmpty();
    }

    #endregion

    #region OnSpecCompletedAsync

    [Test]
    public async Task OnSpecCompletedAsync_CapturesResult()
    {
        var reporter = new ResultCaptureReporter();
        var spec = new SpecDefinition("test spec", () => { });
        var result = new SpecResult(spec, SpecStatus.Passed, [], TimeSpan.FromMilliseconds(100), null);

        await reporter.OnSpecCompletedAsync(result);

        await Assert.That(reporter.Results).Count().IsEqualTo(1);
        await Assert.That(reporter.Results[0].Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task OnSpecCompletedAsync_CapturesMultipleResults()
    {
        var reporter = new ResultCaptureReporter();
        var spec1 = new SpecDefinition("spec 1", () => { });
        var spec2 = new SpecDefinition("spec 2", () => { });
        var spec3 = new SpecDefinition("spec 3", () => { });

        await reporter.OnSpecCompletedAsync(new SpecResult(spec1, SpecStatus.Passed, [], TimeSpan.Zero, null));
        await reporter.OnSpecCompletedAsync(new SpecResult(spec2, SpecStatus.Failed, [], TimeSpan.Zero, new Exception("fail")));
        await reporter.OnSpecCompletedAsync(new SpecResult(spec3, SpecStatus.Pending, [], TimeSpan.Zero, null));

        await Assert.That(reporter.Results).Count().IsEqualTo(3);
        await Assert.That(reporter.Results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(reporter.Results[1].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(reporter.Results[2].Status).IsEqualTo(SpecStatus.Pending);
    }

    [Test]
    public async Task OnSpecCompletedAsync_PreservesResultDetails()
    {
        var reporter = new ResultCaptureReporter();
        var spec = new SpecDefinition("detailed spec", () => { }) { LineNumber = 42 };
        var exception = new InvalidOperationException("test error");
        var contextPath = new[] { "Context", "Nested" };
        var result = new SpecResult(spec, SpecStatus.Failed, contextPath, TimeSpan.FromSeconds(1.5), exception);

        await reporter.OnSpecCompletedAsync(result);

        var captured = reporter.Results[0];
        await Assert.That(captured.Spec.Description).IsEqualTo("detailed spec");
        await Assert.That(captured.Spec.LineNumber).IsEqualTo(42);
        await Assert.That(captured.Exception).IsEqualTo(exception);
        await Assert.That(captured.ContextPath).IsEquivalentTo(contextPath);
        await Assert.That(captured.TotalDuration).IsEqualTo(TimeSpan.FromSeconds(1.5));
    }

    #endregion

    #region OnRunCompletedAsync

    [Test]
    public async Task OnRunCompletedAsync_DoesNotAffectResults()
    {
        var reporter = new ResultCaptureReporter();
        var spec = new SpecDefinition("test", () => { });
        await reporter.OnSpecCompletedAsync(new SpecResult(spec, SpecStatus.Passed, [], TimeSpan.Zero, null));

        await reporter.OnRunCompletedAsync(new SpecReport());

        // Results should still be there
        await Assert.That(reporter.Results).Count().IsEqualTo(1);
    }

    #endregion

    #region Clear

    [Test]
    public async Task Clear_RemovesAllResults()
    {
        var reporter = new ResultCaptureReporter();
        var spec = new SpecDefinition("test", () => { });
        await reporter.OnSpecCompletedAsync(new SpecResult(spec, SpecStatus.Passed, [], TimeSpan.Zero, null));
        await reporter.OnSpecCompletedAsync(new SpecResult(spec, SpecStatus.Failed, [], TimeSpan.Zero, null));

        reporter.Clear();

        await Assert.That(reporter.Results).IsEmpty();
    }

    [Test]
    public async Task Clear_AllowsNewResultsToBeAdded()
    {
        var reporter = new ResultCaptureReporter();
        var spec = new SpecDefinition("test", () => { });
        await reporter.OnSpecCompletedAsync(new SpecResult(spec, SpecStatus.Passed, [], TimeSpan.Zero, null));

        reporter.Clear();

        await reporter.OnSpecCompletedAsync(new SpecResult(spec, SpecStatus.Failed, [], TimeSpan.Zero, null));

        await Assert.That(reporter.Results).Count().IsEqualTo(1);
        await Assert.That(reporter.Results[0].Status).IsEqualTo(SpecStatus.Failed);
    }

    #endregion

    #region Thread Safety

    [Test]
    public async Task Results_ReturnsThreadSafeCopy()
    {
        var reporter = new ResultCaptureReporter();
        var spec = new SpecDefinition("test", () => { });
        await reporter.OnSpecCompletedAsync(new SpecResult(spec, SpecStatus.Passed, [], TimeSpan.Zero, null));

        var results1 = reporter.Results;

        // Add more results
        await reporter.OnSpecCompletedAsync(new SpecResult(spec, SpecStatus.Failed, [], TimeSpan.Zero, null));

        var results2 = reporter.Results;

        // First snapshot should still have 1 result
        await Assert.That(results1).Count().IsEqualTo(1);
        // Second snapshot should have 2 results
        await Assert.That(results2).Count().IsEqualTo(2);
    }

    [Test]
    public async Task ConcurrentAddAndRead_DoesNotThrow()
    {
        var reporter = new ResultCaptureReporter();
        var spec = new SpecDefinition("test", () => { });
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var addTask = Task.Run(async () =>
        {
            for (var i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
            {
                await reporter.OnSpecCompletedAsync(new SpecResult(spec, SpecStatus.Passed, [], TimeSpan.Zero, null));
            }
        }, cts.Token);

        var readTask = Task.Run(() =>
        {
            for (var i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
            {
                _ = reporter.Results;
            }
        }, cts.Token);

        // Should not throw
        await Task.WhenAll(addTask, readTask);

        await Assert.That(reporter.Results.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task ConcurrentClearAndAdd_DoesNotThrow()
    {
        var reporter = new ResultCaptureReporter();
        var spec = new SpecDefinition("test", () => { });

        // No timeout - let the tasks complete naturally
        // The test is about thread safety, not timing
        var addTask = Task.Run(async () =>
        {
            for (var i = 0; i < 100; i++)
            {
                await reporter.OnSpecCompletedAsync(new SpecResult(spec, SpecStatus.Passed, [], TimeSpan.Zero, null));
            }
        });

        var clearTask = Task.Run(() =>
        {
            for (var i = 0; i < 50; i++)
            {
                reporter.Clear();
                Thread.Sleep(10); // Slow down clears to let some adds happen
            }
        });

        // Should not throw
        await Task.WhenAll(addTask, clearTask);
    }

    #endregion
}
