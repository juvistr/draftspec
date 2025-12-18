using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for SpecFileRunner records and related types.
/// </summary>
public class SpecFileRunnerTests
{
    #region SpecRunResult

    [Test]
    public async Task SpecRunResult_ExitCodeZero_IsSuccess()
    {
        var result = new SpecRunResult(
            SpecFile: "test.spec.csx",
            Output: "",
            Error: "",
            ExitCode: 0,
            Duration: TimeSpan.FromMilliseconds(100));

        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task SpecRunResult_ExitCodeNonZero_IsNotSuccess()
    {
        var result = new SpecRunResult(
            SpecFile: "test.spec.csx",
            Output: "",
            Error: "",
            ExitCode: 1,
            Duration: TimeSpan.FromMilliseconds(100));

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task SpecRunResult_NegativeExitCode_IsNotSuccess()
    {
        var result = new SpecRunResult(
            SpecFile: "test.spec.csx",
            Output: "",
            Error: "Terminated",
            ExitCode: -1,
            Duration: TimeSpan.FromMilliseconds(100));

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task SpecRunResult_PreservesAllProperties()
    {
        var duration = TimeSpan.FromSeconds(1.5);
        var result = new SpecRunResult(
            SpecFile: "/path/to/spec.csx",
            Output: "stdout content",
            Error: "stderr content",
            ExitCode: 42,
            Duration: duration);

        await Assert.That(result.SpecFile).IsEqualTo("/path/to/spec.csx");
        await Assert.That(result.Output).IsEqualTo("stdout content");
        await Assert.That(result.Error).IsEqualTo("stderr content");
        await Assert.That(result.ExitCode).IsEqualTo(42);
        await Assert.That(result.Duration).IsEqualTo(duration);
    }

    #endregion

    #region RunSummary

    [Test]
    public async Task RunSummary_AllSuccess_IsSuccess()
    {
        var results = new[]
        {
            new SpecRunResult("a.spec.csx", "", "", 0, TimeSpan.Zero),
            new SpecRunResult("b.spec.csx", "", "", 0, TimeSpan.Zero),
            new SpecRunResult("c.spec.csx", "", "", 0, TimeSpan.Zero)
        };

        var summary = new RunSummary(results, TimeSpan.FromSeconds(1));

        await Assert.That(summary.Success).IsTrue();
    }

    [Test]
    public async Task RunSummary_OneFailure_IsNotSuccess()
    {
        var results = new[]
        {
            new SpecRunResult("a.spec.csx", "", "", 0, TimeSpan.Zero),
            new SpecRunResult("b.spec.csx", "", "", 1, TimeSpan.Zero),
            new SpecRunResult("c.spec.csx", "", "", 0, TimeSpan.Zero)
        };

        var summary = new RunSummary(results, TimeSpan.FromSeconds(1));

        await Assert.That(summary.Success).IsFalse();
    }

    [Test]
    public async Task RunSummary_AllFailures_IsNotSuccess()
    {
        var results = new[]
        {
            new SpecRunResult("a.spec.csx", "", "", 1, TimeSpan.Zero),
            new SpecRunResult("b.spec.csx", "", "", 1, TimeSpan.Zero)
        };

        var summary = new RunSummary(results, TimeSpan.FromSeconds(1));

        await Assert.That(summary.Success).IsFalse();
    }

    [Test]
    public async Task RunSummary_EmptyResults_IsSuccess()
    {
        var summary = new RunSummary([], TimeSpan.Zero);

        await Assert.That(summary.Success).IsTrue();
    }

    [Test]
    public async Task RunSummary_TotalSpecs_EqualsResultCount()
    {
        var results = new[]
        {
            new SpecRunResult("a.spec.csx", "", "", 0, TimeSpan.Zero),
            new SpecRunResult("b.spec.csx", "", "", 0, TimeSpan.Zero),
            new SpecRunResult("c.spec.csx", "", "", 0, TimeSpan.Zero)
        };

        var summary = new RunSummary(results, TimeSpan.FromSeconds(1));

        await Assert.That(summary.TotalSpecs).IsEqualTo(3);
    }

    [Test]
    public async Task RunSummary_PassedCount_IsCorrect()
    {
        var results = new[]
        {
            new SpecRunResult("a.spec.csx", "", "", 0, TimeSpan.Zero),
            new SpecRunResult("b.spec.csx", "", "", 1, TimeSpan.Zero),
            new SpecRunResult("c.spec.csx", "", "", 0, TimeSpan.Zero)
        };

        var summary = new RunSummary(results, TimeSpan.FromSeconds(1));

        await Assert.That(summary.Passed).IsEqualTo(2);
    }

    [Test]
    public async Task RunSummary_FailedCount_IsCorrect()
    {
        var results = new[]
        {
            new SpecRunResult("a.spec.csx", "", "", 0, TimeSpan.Zero),
            new SpecRunResult("b.spec.csx", "", "", 1, TimeSpan.Zero),
            new SpecRunResult("c.spec.csx", "", "", 1, TimeSpan.Zero)
        };

        var summary = new RunSummary(results, TimeSpan.FromSeconds(1));

        await Assert.That(summary.Failed).IsEqualTo(2);
    }

    [Test]
    public async Task RunSummary_PassedPlusFailed_EqualsTotalSpecs()
    {
        var results = new[]
        {
            new SpecRunResult("a.spec.csx", "", "", 0, TimeSpan.Zero),
            new SpecRunResult("b.spec.csx", "", "", 1, TimeSpan.Zero),
            new SpecRunResult("c.spec.csx", "", "", 0, TimeSpan.Zero),
            new SpecRunResult("d.spec.csx", "", "", 1, TimeSpan.Zero)
        };

        var summary = new RunSummary(results, TimeSpan.FromSeconds(1));

        await Assert.That(summary.Passed + summary.Failed).IsEqualTo(summary.TotalSpecs);
    }

    [Test]
    public async Task RunSummary_TotalDuration_IsPreserved()
    {
        var duration = TimeSpan.FromSeconds(5.5);
        var results = new[] { new SpecRunResult("a.spec.csx", "", "", 0, TimeSpan.Zero) };

        var summary = new RunSummary(results, duration);

        await Assert.That(summary.TotalDuration).IsEqualTo(duration);
    }

    #endregion

    #region BuildResult

    [Test]
    public async Task BuildResult_Success_PreservesValue()
    {
        var result = new BuildResult(Success: true, Output: "", Error: "");

        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task BuildResult_Failure_PreservesValue()
    {
        var result = new BuildResult(Success: false, Output: "", Error: "Build failed");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo("Build failed");
    }

    [Test]
    public async Task BuildResult_OutputAndError_Preserved()
    {
        var result = new BuildResult(
            Success: true,
            Output: "Build succeeded.",
            Error: "warning CS0168");

        await Assert.That(result.Output).IsEqualTo("Build succeeded.");
        await Assert.That(result.Error).IsEqualTo("warning CS0168");
    }

    [Test]
    public async Task BuildResult_Skipped_DefaultsToFalse()
    {
        var result = new BuildResult(Success: true, Output: "", Error: "");

        await Assert.That(result.Skipped).IsFalse();
    }

    [Test]
    public async Task BuildResult_Skipped_CanBeSet()
    {
        var result = new BuildResult(Success: true, Output: "", Error: "", Skipped: true);

        await Assert.That(result.Skipped).IsTrue();
    }

    #endregion

    #region ISpecFileRunner Interface

    [Test]
    public async Task SpecFileRunner_ImplementsInterface()
    {
        var runner = new SpecFileRunner();

        await Assert.That(runner).IsAssignableTo<ISpecFileRunner>();
    }

    #endregion

    #region Build Cache

    [Test]
    public async Task ClearBuildCache_DoesNotThrow()
    {
        var runner = new SpecFileRunner();

        // Should not throw even when cache is empty
        runner.ClearBuildCache();

        await Task.CompletedTask;
    }

    [Test]
    public async Task SpecFileRunner_NoCache_CanBeConstructed()
    {
        var runner = new SpecFileRunner(noCache: true);

        await Assert.That(runner).IsNotNull();
    }

    #endregion
}
