using DraftSpec.TestingPlatform;
using DraftSpec.Tests.Infrastructure.Mocks;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace DraftSpec.Tests.TestingPlatform;

/// <summary>
/// Tests for TestOrchestrator.
/// Verifies the orchestration logic that coordinates discovery and execution.
/// </summary>
public class TestOrchestratorTests
{
    private MockSpecDiscoverer _discoverer = null!;
    private MockMtpSpecExecutor _executor = null!;
    private MockTestNodePublisher _publisher = null!;
    private TestOrchestrator _orchestrator = null!;

    [Before(Test)]
    public void Setup()
    {
        _discoverer = new MockSpecDiscoverer();
        _executor = new MockMtpSpecExecutor();
        _publisher = new MockTestNodePublisher();
        _orchestrator = new TestOrchestrator(_discoverer, _executor);
    }

    #region Constructor

    [Test]
    public void Constructor_NullDiscoverer_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TestOrchestrator(null!, _executor));
    }

    [Test]
    public void Constructor_NullExecutor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TestOrchestrator(_discoverer, null!));
    }

    #endregion

    #region DiscoverTestsAsync

    [Test]
    public async Task DiscoverTestsAsync_NullPublisher_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _orchestrator.DiscoverTestsAsync(null!));
    }

    [Test]
    public async Task DiscoverTestsAsync_NoSpecs_PublishesNothing()
    {
        // Default mock returns empty specs

        await _orchestrator.DiscoverTestsAsync(_publisher);

        await Assert.That(_publisher.PublishedNodes.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DiscoverTestsAsync_WithSpecs_PublishesAllSpecs()
    {
        _discoverer.WithSpecs(
            CreateSpec("spec1", "Test spec 1"),
            CreateSpec("spec2", "Test spec 2"),
            CreateSpec("spec3", "Test spec 3"));

        await _orchestrator.DiscoverTestsAsync(_publisher);

        await Assert.That(_publisher.PublishedNodes.Count).IsEqualTo(3);
    }

    [Test]
    public async Task DiscoverTestsAsync_WithSpecs_PublishesCorrectDisplayNames()
    {
        _discoverer.WithSpecs(CreateSpec("spec1", "should work correctly"));

        await _orchestrator.DiscoverTestsAsync(_publisher);

        await Assert.That(_publisher.PublishedNodes[0].DisplayName).IsEqualTo("should work correctly");
    }

    [Test]
    public async Task DiscoverTestsAsync_WithErrors_PublishesErrorNodes()
    {
        _discoverer.WithErrors(
            new DiscoveryError
            {
                SourceFile = "/path/to/file1.csx",
                RelativeSourceFile = "file1.csx",
                Message = "Compilation failed"
            },
            new DiscoveryError
            {
                SourceFile = "/path/to/file2.csx",
                RelativeSourceFile = "file2.csx",
                Message = "Syntax error"
            });

        await _orchestrator.DiscoverTestsAsync(_publisher);

        await Assert.That(_publisher.PublishedNodes.Count).IsEqualTo(2);
    }

    [Test]
    public async Task DiscoverTestsAsync_WithSpecsAndErrors_PublishesBoth()
    {
        _discoverer
            .WithSpecs(CreateSpec("spec1", "Test spec"))
            .WithErrors(new DiscoveryError
            {
                SourceFile = "/path/to/file1.csx",
                RelativeSourceFile = "file1.csx",
                Message = "Failed"
            });

        await _orchestrator.DiscoverTestsAsync(_publisher);

        await Assert.That(_publisher.PublishedNodes.Count).IsEqualTo(2);
    }

    [Test]
    public async Task DiscoverTestsAsync_CallsDiscoverer()
    {
        await _orchestrator.DiscoverTestsAsync(_publisher);

        await Assert.That(_discoverer.DiscoverAsyncCalls.Count).IsEqualTo(1);
    }

    #endregion

    #region RunTestsAsync - All Tests

    [Test]
    public async Task RunTestsAsync_NullPublisher_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _orchestrator.RunTestsAsync(null, null!));
    }

    [Test]
    public async Task RunTestsAsync_NoFilter_RunsAllFiles()
    {
        _discoverer.WithSpecs(
            CreateSpec("spec1", "Test 1", "/path/to/file1.csx"),
            CreateSpec("spec2", "Test 2", "/path/to/file2.csx"));

        _executor
            .WithFileResult("/path/to/file1.csx", CreateExecutionResult("file1.csx"))
            .WithFileResult("/path/to/file2.csx", CreateExecutionResult("file2.csx"));

        await _orchestrator.RunTestsAsync(null, _publisher);

        await Assert.That(_executor.ExecuteFileAsyncCalls.Count).IsEqualTo(2);
    }

    [Test]
    public async Task RunTestsAsync_NoFilter_PublishesResults()
    {
        _discoverer.WithSpecs(CreateSpec("spec1", "Test 1", "/path/to/file.csx"));
        _executor.WithFileResult("/path/to/file.csx", CreateExecutionResult("file.csx", 2));

        await _orchestrator.RunTestsAsync(null, _publisher);

        await Assert.That(_publisher.PublishedNodes.Count).IsEqualTo(2);
    }

    [Test]
    public async Task RunTestsAsync_NoFilter_PublishesDiscoveryErrors()
    {
        _discoverer
            .WithSpecs(CreateSpec("spec1", "Test 1", "/path/to/file.csx"))
            .WithErrors(new DiscoveryError
            {
                SourceFile = "/path/to/bad.csx",
                RelativeSourceFile = "bad.csx",
                Message = "Failed"
            });
        _executor.WithFileResult("/path/to/file.csx", CreateExecutionResult("file.csx"));

        await _orchestrator.RunTestsAsync(null, _publisher);

        // 1 for the discovery error + 1 for the execution result
        await Assert.That(_publisher.PublishedNodes.Count).IsEqualTo(2);
    }

    [Test]
    public async Task RunTestsAsync_WithCompilationErrors_PublishesAsFailedTests()
    {
        _discoverer.WithSpecs(
            CreateSpec("spec1", "Test 1", "/path/to/file.csx"),
            CreateSpecWithCompilationError("spec2", "Test 2", "Compilation failed"));
        _executor.WithFileResult("/path/to/file.csx", CreateExecutionResult("file.csx"));

        await _orchestrator.RunTestsAsync(null, _publisher);

        // 1 for the compilation error spec + 1 for the execution result
        await Assert.That(_publisher.PublishedNodes.Count).IsEqualTo(2);
    }

    [Test]
    public async Task RunTestsAsync_WithCompilationErrors_SkipsExecutionOfBrokenSpecs()
    {
        _discoverer.WithSpecs(
            CreateSpecWithCompilationError("spec1", "Test 1", "Compilation failed"));

        await _orchestrator.RunTestsAsync(null, _publisher);

        // Executor should not be called for specs with compilation errors
        await Assert.That(_executor.ExecuteFileAsyncCalls.Count).IsEqualTo(0);
    }

    #endregion

    #region RunTestsAsync - Filtered by IDs

    [Test]
    public async Task RunTestsAsync_WithFilter_OnlyRunsRequestedSpecs()
    {
        _discoverer.WithSpecs(
            CreateSpec("spec1", "Test 1"),
            CreateSpec("spec2", "Test 2"),
            CreateSpec("spec3", "Test 3"));
        _executor.WithIdResults(CreateExecutionResult("file.csx"));

        var filter = new HashSet<string> { "spec1", "spec3" };
        await _orchestrator.RunTestsAsync(filter, _publisher);

        await Assert.That(_executor.ExecuteByIdsAsyncCalls.Count).IsEqualTo(1);
        await Assert.That(_executor.ExecuteByIdsAsyncCalls[0].Count()).IsEqualTo(2);
    }

    [Test]
    public async Task RunTestsAsync_WithFilter_IgnoresUnknownIds()
    {
        _discoverer.WithSpecs(CreateSpec("spec1", "Test 1"));
        _executor.WithIdResults(CreateExecutionResult("file.csx"));

        var filter = new HashSet<string> { "spec1", "unknown-spec" };
        await _orchestrator.RunTestsAsync(filter, _publisher);

        await Assert.That(_executor.ExecuteByIdsAsyncCalls.Count).IsEqualTo(1);
        var requestedIds = _executor.ExecuteByIdsAsyncCalls[0].ToList();
        await Assert.That(requestedIds.Count).IsEqualTo(1);
        await Assert.That(requestedIds[0]).IsEqualTo("spec1");
    }

    [Test]
    public async Task RunTestsAsync_WithEmptyFilter_TreatsAsRunAll()
    {
        _discoverer.WithSpecs(CreateSpec("spec1", "Test 1", "/path/to/file.csx"));
        _executor.WithFileResult("/path/to/file.csx", CreateExecutionResult("file.csx"));

        var filter = new HashSet<string>();
        await _orchestrator.RunTestsAsync(filter, _publisher);

        // Empty filter should run all specs via ExecuteFileAsync
        await Assert.That(_executor.ExecuteFileAsyncCalls.Count).IsEqualTo(1);
    }

    [Test]
    public async Task RunTestsAsync_FilteredWithCompilationErrors_ReportsErrors()
    {
        _discoverer.WithSpecs(
            CreateSpecWithCompilationError("spec1", "Test 1", "Syntax error"));

        var filter = new HashSet<string> { "spec1" };
        await _orchestrator.RunTestsAsync(filter, _publisher);

        // Should publish error for the broken spec
        await Assert.That(_publisher.PublishedNodes.Count).IsEqualTo(1);
        // Executor should not be called
        await Assert.That(_executor.ExecuteByIdsAsyncCalls.Count).IsEqualTo(0);
    }

    #endregion

    #region Helper Methods

    private static DiscoveredSpec CreateSpec(string id, string description, string sourceFile = "/path/to/test.spec.csx")
    {
        return new DiscoveredSpec
        {
            Id = id,
            Description = description,
            DisplayName = description,
            ContextPath = ["TestContext"],
            SourceFile = sourceFile,
            RelativeSourceFile = Path.GetFileName(sourceFile)
        };
    }

    private static DiscoveredSpec CreateSpecWithCompilationError(string id, string description, string error)
    {
        return new DiscoveredSpec
        {
            Id = id,
            Description = description,
            DisplayName = description,
            ContextPath = ["TestContext"],
            SourceFile = "/path/to/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            CompilationError = error
        };
    }

    private static ExecutionResult CreateExecutionResult(string sourceFile, int specCount = 1)
    {
        var results = new List<SpecResult>();
        for (var i = 0; i < specCount; i++)
        {
            results.Add(new SpecResult(
                new SpecDefinition($"spec{i}", () => { }),
                SpecStatus.Passed,
                ["TestContext"],
                TimeSpan.FromMilliseconds(10),
                null));
        }

        return new ExecutionResult(sourceFile, $"/path/to/{sourceFile}", results);
    }

    #endregion
}
