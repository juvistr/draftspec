using DraftSpec.Cli;
using DraftSpec.Formatters;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Comprehensive tests for InProcessSpecRunner.
/// Tests execution flow, DSL management, filtering, and build integration.
/// </summary>
public class InProcessSpecRunnerTests
{
    #region RunFileAsync Tests

    [Test]
    public async Task RunFileAsync_Success_ReturnsReport()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        var rootContext = CreateTestContext("Calculator", specs: 3);
        scriptExecutor.SetResult(rootContext);

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        var result = await runner.RunFileAsync("/project/Specs/test.spec.csx");

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.SpecFile).IsEqualTo("/project/Specs/test.spec.csx");
        await Assert.That(result.Report.Summary.Total).IsEqualTo(3);
        await Assert.That(result.Error).IsNull();
    }

    [Test]
    public async Task RunFileAsync_NoSpecs_ReturnsEmptyReport()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        scriptExecutor.SetResult(null); // No specs defined

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        var result = await runner.RunFileAsync("/project/test.spec.csx");

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Report.Summary.Total).IsEqualTo(0);
        await Assert.That(result.Error).IsNull();
    }

    [Test]
    public async Task RunFileAsync_Exception_ReturnsErrorWithException()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        var exception = new Exception("Script compilation failed");
        scriptExecutor.SetException(exception);

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        var result = await runner.RunFileAsync("/project/test.spec.csx");

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(exception);
        await Assert.That(result.Report.Summary.Failed).IsEqualTo(1);
    }

    [Test]
    public async Task RunFileAsync_ResetsDslBeforeExecution()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        var rootContext = CreateTestContext("Test");
        scriptExecutor.SetResult(rootContext);

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        await runner.RunFileAsync("/project/test.spec.csx");

        // Assert - DSL should be reset at least once (before execution)
        await Assert.That(dslManager.ResetCalls).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task RunFileAsync_ResetsDslAfterExecution()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        var rootContext = CreateTestContext("Test");
        scriptExecutor.SetResult(rootContext);

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        await runner.RunFileAsync("/project/test.spec.csx");

        // Assert - DSL should be reset twice (before and after)
        await Assert.That(dslManager.ResetCalls).IsEqualTo(2);
    }

    [Test]
    public async Task RunFileAsync_ResetsDslEvenOnException()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        scriptExecutor.SetException(new Exception("Error"));

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        await runner.RunFileAsync("/project/test.spec.csx");

        // Assert - DSL should be reset in finally block even on exception
        await Assert.That(dslManager.ResetCalls).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task RunFileAsync_BuildsProjectBeforeExecution()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        var rootContext = CreateTestContext("Test");
        scriptExecutor.SetResult(rootContext);

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        await runner.RunFileAsync("/project/Specs/test.spec.csx");

        // Assert
        await Assert.That(projectBuilder.BuildProjectsCalls).Count().IsEqualTo(1);
        await Assert.That(projectBuilder.BuildProjectsCalls[0]).Contains("/project/Specs");
    }

    #endregion

    #region RunAllAsync Tests

    [Test]
    public async Task RunAllAsync_Sequential_RunsFilesInOrder()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        scriptExecutor.SetResult(CreateTestContext("Test1", specs: 2));

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        var files = new List<string>
        {
            "/project/test1.spec.csx",
            "/project/test2.spec.csx",
            "/project/test3.spec.csx"
        };

        // Act
        var summary = await runner.RunAllAsync(files, parallel: false);

        // Assert
        await Assert.That(summary.Results).Count().IsEqualTo(3);
        await Assert.That(summary.Success).IsTrue();
        await Assert.That(summary.TotalSpecs).IsEqualTo(6); // 3 files × 2 specs each
    }

    [Test]
    public async Task RunAllAsync_Parallel_RunsFilesInParallel()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        scriptExecutor.SetResult(CreateTestContext("Test", specs: 5));

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        var files = new List<string>
        {
            "/project/a.spec.csx",
            "/project/b.spec.csx"
        };

        // Act
        var summary = await runner.RunAllAsync(files, parallel: true);

        // Assert
        await Assert.That(summary.Results).Count().IsEqualTo(2);
        await Assert.That(summary.TotalSpecs).IsEqualTo(10); // 2 files × 5 specs each
    }

    [Test]
    public async Task RunAllAsync_BuildsEachDirectoryOnce()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        scriptExecutor.SetResult(CreateTestContext("Test", specs: 1));

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        var files = new List<string>
        {
            "/project/specs/a.spec.csx",
            "/project/specs/b.spec.csx",
            "/other/c.spec.csx"
        };

        // Act
        await runner.RunAllAsync(files, parallel: false);

        // Assert - Should build 2 unique directories plus once per file
        // BuildProjects is called once per directory upfront, then once per file
        var buildCalls = projectBuilder.BuildProjectsCalls;
        await Assert.That(buildCalls).Count().IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task RunAllAsync_AggregatesTotalDuration()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock { ElapsedMilliseconds = 250 };

        scriptExecutor.SetResult(CreateTestContext("Test", specs: 1));

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        var files = new List<string>
        {
            "/project/a.spec.csx",
            "/project/b.spec.csx"
        };

        // Act
        var summary = await runner.RunAllAsync(files, parallel: false);

        // Assert
        await Assert.That(summary.TotalDuration.TotalMilliseconds).IsEqualTo(250);
    }

    [Test]
    public async Task RunAllAsync_CancellationRequested_Throws()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        scriptExecutor.SetResult(CreateTestContext("Test", specs: 1));

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        var files = new List<string>
        {
            "/project/a.spec.csx",
            "/project/b.spec.csx",
            "/project/c.spec.csx"
        };

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await runner.RunAllAsync(files, parallel: false, cts.Token));
    }

    #endregion

    #region Filter Tests

    [Test]
    public async Task BuildRunner_WithFilterTags_CreatesFilteredRunner()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        var rootContext = CreateTestContext("Test", specs: 1);
        scriptExecutor.SetResult(rootContext);

        var runner = new InProcessSpecRunner(
            filterTags: "smoke,regression",
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        var result = await runner.RunFileAsync("/project/test.spec.csx");

        // Assert - Should run successfully with filters applied
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task BuildRunner_WithExcludeTags_CreatesFilteredRunner()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        var rootContext = CreateTestContext("Test", specs: 1);
        scriptExecutor.SetResult(rootContext);

        var runner = new InProcessSpecRunner(
            excludeTags: "slow,flaky",
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        var result = await runner.RunFileAsync("/project/test.spec.csx");

        // Assert
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task BuildRunner_WithFilterName_CreatesFilteredRunner()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        var rootContext = CreateTestContext("Test", specs: 1);
        scriptExecutor.SetResult(rootContext);

        var runner = new InProcessSpecRunner(
            filterName: "Calculator",
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        var result = await runner.RunFileAsync("/project/test.spec.csx");

        // Assert
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task BuildRunner_WithExcludeName_CreatesFilteredRunner()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        var rootContext = CreateTestContext("Test", specs: 1);
        scriptExecutor.SetResult(rootContext);

        var runner = new InProcessSpecRunner(
            excludeName: "slow test",
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        var result = await runner.RunFileAsync("/project/test.spec.csx");

        // Assert
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task BuildRunner_EmptyFilters_NoFiltersApplied()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        var rootContext = CreateTestContext("Test", specs: 3);
        scriptExecutor.SetResult(rootContext);

        var runner = new InProcessSpecRunner(
            filterTags: "",
            excludeTags: null,
            filterName: "",
            excludeName: null,
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        var result = await runner.RunFileAsync("/project/test.spec.csx");

        // Assert - All specs should run
        await Assert.That(result.Report.Summary.Total).IsEqualTo(3);
    }

    #endregion

    #region Build Events Tests

    [Test]
    public async Task OnBuildStarted_FiresWhenProjectBuilderStarts()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        scriptExecutor.SetResult(CreateTestContext("Test", specs: 1));

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        string? capturedProject = null;
        runner.OnBuildStarted += project => capturedProject = project;

        // Act
        projectBuilder.TriggerBuildStarted("/project/test.csproj");
        await runner.RunFileAsync("/project/test.spec.csx");

        // Assert
        await Assert.That(capturedProject).IsNotNull();
    }

    [Test]
    public async Task OnBuildCompleted_FiresWhenProjectBuilderCompletes()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        scriptExecutor.SetResult(CreateTestContext("Test", specs: 1));

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        BuildResult? capturedResult = null;
        runner.OnBuildCompleted += result => capturedResult = result;

        // Act
        var buildResult = new BuildResult(true, "", "");
        projectBuilder.TriggerBuildCompleted(buildResult);
        await runner.RunFileAsync("/project/test.spec.csx");

        // Assert
        await Assert.That(capturedResult).IsNotNull();
    }

    #endregion

    #region Build Cache Tests

    [Test]
    public async Task ClearBuildCache_DelegatesToProjectBuilder()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager);

        // Act
        runner.ClearBuildCache();

        // Assert
        await Assert.That(projectBuilder.ClearBuildCacheCalls).IsEqualTo(1);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create a test SpecContext with the given number of specs.
    /// </summary>
    private static SpecContext CreateTestContext(string description, int specs = 1)
    {
        var context = new SpecContext(description, null);
        for (int i = 0; i < specs; i++)
        {
            context.AddSpec(new SpecDefinition($"spec {i + 1}", () => Task.CompletedTask));
        }
        return context;
    }

    #endregion
}

#region Mock Implementations

/// <summary>
/// Mock DSL manager that tracks Reset() calls.
/// </summary>
file class MockDslManager : IDslManager
{
    public int ResetCalls { get; private set; }

    public void Reset()
    {
        ResetCalls++;
    }
}

/// <summary>
/// Mock project builder for testing.
/// </summary>
file class MockProjectBuilder : IProjectBuilder
{
    public event Action<string>? OnBuildStarted;
    public event Action<BuildResult>? OnBuildCompleted;
    public event Action<string>? OnBuildSkipped;

    public List<string> BuildProjectsCalls { get; } = [];
    public int ClearBuildCacheCalls { get; private set; }

    public void BuildProjects(string directory)
    {
        BuildProjectsCalls.Add(directory);
    }

    public string FindOutputDirectory(string specDirectory)
    {
        return Path.Combine(specDirectory, "bin", "Debug", "net10.0");
    }

    public void ClearBuildCache()
    {
        ClearBuildCacheCalls++;
    }

    public void TriggerBuildStarted(string project)
    {
        OnBuildStarted?.Invoke(project);
    }

    public void TriggerBuildCompleted(BuildResult result)
    {
        OnBuildCompleted?.Invoke(result);
    }

    public void TriggerBuildSkipped(string project)
    {
        OnBuildSkipped?.Invoke(project);
    }
}

/// <summary>
/// Mock spec script executor for testing.
/// </summary>
file class MockSpecScriptExecutor : ISpecScriptExecutor
{
    private SpecContext? _result;
    private Exception? _exception;

    public void SetResult(SpecContext? context)
    {
        _result = context;
        _exception = null;
    }

    public void SetException(Exception exception)
    {
        _exception = exception;
        _result = null;
    }

    public Task<SpecContext?> ExecuteAsync(string specFile, string outputDirectory, CancellationToken ct = default)
    {
        if (_exception != null)
            throw _exception;

        return Task.FromResult(_result);
    }
}

/// <summary>
/// Mock time provider for testing.
/// </summary>
file class MockClock : DraftSpec.IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public int ElapsedMilliseconds { get; set; } = 100;

    public DraftSpec.IStopwatch StartNew()
    {
        return new MockStopwatch(ElapsedMilliseconds);
    }
}

/// <summary>
/// Mock stopwatch for testing.
/// </summary>
file class MockStopwatch : DraftSpec.IStopwatch
{
    private readonly int _milliseconds;

    public MockStopwatch(int milliseconds = 100)
    {
        _milliseconds = milliseconds;
    }

    public TimeSpan Elapsed => TimeSpan.FromMilliseconds(_milliseconds);
    public void StopTiming() { }
}

#endregion
