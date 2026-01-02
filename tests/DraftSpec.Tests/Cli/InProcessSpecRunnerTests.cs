using System.Collections.Immutable;
using DraftSpec.Cli;
using DraftSpec.Formatters;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

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
        var specPath = TestPaths.Project("Specs/test.spec.csx");
        var result = await runner.RunFileAsync(specPath);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.SpecFile).IsEqualTo(specPath);
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
        var result = await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

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
        var result = await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(exception);
        await Assert.That(result.Report.Summary.Failed).IsEqualTo(1);
    }

    [Test]
    public async Task RunFileAsync_CompilationErrorException_ReturnsCompilationDiagnosticException()
    {
        // Arrange
        var dslManager = new MockDslManager();
        var projectBuilder = new MockProjectBuilder();
        var scriptExecutor = new MockSpecScriptExecutor();
        var timeProvider = new MockClock();
        var diagnosticFormatter = new MockCompilationDiagnosticFormatter();
        var fileSystem = new MockFileSystem();

        // Create a real CompilationErrorException
        var compilationException = CreateCompilationError("var x = ");
        scriptExecutor.SetException(compilationException);

        var runner = new InProcessSpecRunner(
            timeProvider: timeProvider,
            projectBuilder: projectBuilder,
            scriptExecutor: scriptExecutor,
            dslManager: dslManager,
            diagnosticFormatter: diagnosticFormatter,
            fileSystem: fileSystem);

        // Act
        var result = await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsAssignableTo<CompilationDiagnosticException>();
        await Assert.That(result.Report.Summary.Failed).IsEqualTo(1);

        var diagnosticException = (CompilationDiagnosticException)result.Error!;
        await Assert.That(diagnosticException.FormattedMessage).IsEqualTo("Formatted compilation error");
        await Assert.That(diagnosticException.CompilationError).IsSameReferenceAs(compilationException);
    }

    /// <summary>
    /// Creates a real CompilationErrorException from invalid code.
    /// </summary>
    private static CompilationErrorException CreateCompilationError(string code)
    {
        try
        {
            var script = CSharpScript.Create(code);
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (diagnostics.Count > 0)
            {
                return new CompilationErrorException(
                    "Compilation failed",
                    diagnostics.ToImmutableArray());
            }

            // Force evaluate to get runtime compilation errors
            script.RunAsync().GetAwaiter().GetResult();
            throw new InvalidOperationException("Expected compilation error");
        }
        catch (CompilationErrorException ex)
        {
            return ex;
        }
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
        await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

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
        await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

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
        await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

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

        var specPath = TestPaths.Project("Specs/test.spec.csx");

        // Act
        await runner.RunFileAsync(specPath);

        // Assert
        await Assert.That(projectBuilder.BuildProjectsCalls).Count().IsEqualTo(1);
        await Assert.That(projectBuilder.BuildProjectsCalls[0]).Contains(TestPaths.Project("Specs"));
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
            TestPaths.Project("test1.spec.csx"),
            TestPaths.Project("test2.spec.csx"),
            TestPaths.Project("test3.spec.csx")
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
            TestPaths.Project("a.spec.csx"),
            TestPaths.Project("b.spec.csx")
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
            TestPaths.Project("specs/a.spec.csx"),
            TestPaths.Project("specs/b.spec.csx"),
            TestPaths.Temp("other/c.spec.csx")
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
            TestPaths.Project("a.spec.csx"),
            TestPaths.Project("b.spec.csx")
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
            TestPaths.Project("a.spec.csx"),
            TestPaths.Project("b.spec.csx"),
            TestPaths.Project("c.spec.csx")
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
        var result = await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

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
        var result = await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

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
        var result = await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

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
        var result = await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

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
        var result = await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

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
        projectBuilder.TriggerBuildStarted(TestPaths.Project("test.csproj"));
        await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

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
        await runner.RunFileAsync(TestPaths.Project("test.spec.csx"));

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
