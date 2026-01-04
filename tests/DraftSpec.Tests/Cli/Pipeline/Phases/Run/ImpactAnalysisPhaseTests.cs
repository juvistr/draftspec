using DraftSpec.Cli.DependencyGraph;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Run;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Run;

/// <summary>
/// Tests for <see cref="ImpactAnalysisPhase"/>.
/// </summary>
public class ImpactAnalysisPhaseTests
{
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;
    private MockGitService _gitService = null!;
    private MockPathComparer _pathComparer = null!;
    private MockDependencyGraphBuilder _graphBuilder = null!;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
        _gitService = new MockGitService();
        _pathComparer = new MockPathComparer();
        _graphBuilder = new MockDependencyGraphBuilder();
    }

    #region No AffectedBy Tests

    [Test]
    public async Task ExecuteAsync_NoAffectedBy_PassesThroughUnchanged()
    {
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var context = CreateContext();
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsTrue();
        await Assert.That(_gitService.GetChangedFilesCalls).IsEmpty();
    }

    [Test]
    public async Task ExecuteAsync_EmptyAffectedBy_PassesThroughUnchanged()
    {
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var context = CreateContext(affectedBy: "");
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsTrue();
    }

    #endregion

    #region No Spec Files Tests

    [Test]
    public async Task ExecuteAsync_NoSpecFiles_ReturnsZero()
    {
        _gitService.WithChangedFiles(TestPaths.Project("src/Service.cs"));
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: []);
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsFalse();
        await Assert.That(_console.Output).Contains("No spec files to analyze");
    }

    [Test]
    public async Task ExecuteAsync_NullSpecFiles_ReturnsZero()
    {
        _gitService.WithChangedFiles(TestPaths.Project("src/Service.cs"));
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var context = CreateContext(affectedBy: "HEAD~1");
        // Don't set SpecFiles - it will be null
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsFalse();
    }

    #endregion

    #region Git Error Tests

    [Test]
    public async Task ExecuteAsync_GitError_ReturnsError()
    {
        _gitService.ThrowsOnGetChangedFiles(new InvalidOperationException("Git not found"));
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var specFiles = new List<string> { TestPaths.Project("specs/test.spec.csx") };
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: specFiles);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("Failed to get changed files");
        await Assert.That(_console.Errors).Contains("Git not found");
    }

    #endregion

    #region No Changed Files Tests

    [Test]
    public async Task ExecuteAsync_NoChangedFiles_ReturnsZero()
    {
        _gitService.WithChangedFiles(); // empty
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var specFiles = new List<string> { TestPaths.Project("specs/test.spec.csx") };
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: specFiles);
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsFalse();
        await Assert.That(_console.Output).Contains("No changed files detected");
    }

    #endregion

    #region Git Service Integration Tests

    [Test]
    public async Task ExecuteAsync_CallsGitServiceCorrectly()
    {
        _gitService.WithChangedFiles(); // empty - avoids DependencyGraphBuilder
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var specFiles = new List<string> { TestPaths.Project("specs/test.spec.csx") };
        var context = CreateContext(affectedBy: "staged", specFiles: specFiles);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_gitService.GetChangedFilesCalls).Count().IsEqualTo(1);
        await Assert.That(_gitService.GetChangedFilesCalls[0].Reference).IsEqualTo("staged");
        await Assert.That(_gitService.GetChangedFilesCalls[0].WorkingDirectory).IsEqualTo(TestPaths.ProjectDir);
    }

    #endregion

    #region Console Output Tests

    [Test]
    public async Task ExecuteAsync_OutputsImpactAnalysisMessage()
    {
        _gitService.WithChangedFiles(); // empty
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var specFiles = new List<string> { TestPaths.Project("specs/test.spec.csx") };
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: specFiles);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("Analyzing impact of changes: HEAD~1");
    }

    #endregion

    #region Dependency Graph Tests

    [Test]
    public async Task ExecuteAsync_WithChangedFiles_BuildsDependencyGraph()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _gitService.WithChangedFiles(TestPaths.Project("src/Service.cs"));
        var graph = CreateGraphWithAffectedSpecs(specFile);
        _graphBuilder.WithGraph(graph);

        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var specFiles = new List<string> { specFile };
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: specFiles);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_graphBuilder.BuildAsyncCalls).Count().IsEqualTo(1);
        await Assert.That(_graphBuilder.BuildAsyncCalls[0].SpecDirectory).IsEqualTo(TestPaths.ProjectDir);
    }

    [Test]
    public async Task ExecuteAsync_AffectedSpecs_FiltersToMatchingSpecs()
    {
        var specFile1 = TestPaths.Project("specs/affected.spec.csx");
        var specFile2 = TestPaths.Project("specs/unaffected.spec.csx");
        _gitService.WithChangedFiles(TestPaths.Project("src/Service.cs"));
        var graph = CreateGraphWithAffectedSpecs(specFile1); // Only specFile1 is affected
        _graphBuilder.WithGraph(graph);

        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var specFiles = new List<string> { specFile1, specFile2 };
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: specFiles);
        var pipelineCalled = false;
        IReadOnlyList<string>? filteredSpecs = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                pipelineCalled = true;
                filteredSpecs = ctx.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsTrue();
        await Assert.That(filteredSpecs).IsNotNull();
        await Assert.That(filteredSpecs!).Count().IsEqualTo(1);
        await Assert.That(filteredSpecs![0]).IsEqualTo(specFile1);
    }

    [Test]
    public async Task ExecuteAsync_NoAffectedSpecs_ReturnsZeroWithoutCallingPipeline()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _gitService.WithChangedFiles(TestPaths.Project("src/Unrelated.cs"));
        var graph = new DraftSpec.Cli.DependencyGraph.DependencyGraph(_pathComparer); // Empty graph - no affected specs
        _graphBuilder.WithGraph(graph);

        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var specFiles = new List<string> { specFile };
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: specFiles);
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsFalse();
        await Assert.That(_console.Output).Contains("No affected specs to run");
    }

    [Test]
    public async Task ExecuteAsync_OutputsImpactSummary()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _gitService.WithChangedFiles(TestPaths.Project("src/Service.cs"));
        var graph = CreateGraphWithAffectedSpecs(specFile);
        _graphBuilder.WithGraph(graph);

        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var specFiles = new List<string> { specFile };
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: specFiles);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("Changed files: 1");
        await Assert.That(_console.Output).Contains("Affected specs: 1 of 1");
    }

    #endregion

    #region Dry Run Tests

    [Test]
    public async Task ExecuteAsync_DryRun_OutputsAffectedSpecsWithoutRunning()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _gitService.WithChangedFiles(TestPaths.Project("src/Service.cs"));
        var graph = CreateGraphWithAffectedSpecs(specFile);
        _graphBuilder.WithGraph(graph);

        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var specFiles = new List<string> { specFile };
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: specFiles, dryRun: true);
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsFalse();
        await Assert.That(_console.Output).Contains("Affected spec files (dry run)");
    }

    [Test]
    public async Task ExecuteAsync_DryRun_ListsAffectedSpecs()
    {
        var specFile1 = TestPaths.Project("specs/a.spec.csx");
        var specFile2 = TestPaths.Project("specs/b.spec.csx");
        _gitService.WithChangedFiles(TestPaths.Project("src/Service.cs"));
        var graph = CreateGraphWithAffectedSpecs(specFile1, specFile2);
        _graphBuilder.WithGraph(graph);

        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var specFiles = new List<string> { specFile1, specFile2 };
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: specFiles, dryRun: true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("a.spec.csx");
        await Assert.That(_console.Output).Contains("b.spec.csx");
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task ExecuteAsync_NoProjectPath_ReturnsError()
    {
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer, _graphBuilder);
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.AffectedBy, "HEAD~1");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("ProjectPath not set");
    }

    #endregion

    #region Helper Methods

    private CommandContext CreateContext(
        string? affectedBy = null,
        string? projectPath = null,
        IReadOnlyList<string>? specFiles = null,
        bool dryRun = false)
    {
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.ProjectPath, projectPath ?? TestPaths.ProjectDir);

        if (affectedBy != null)
            context.Set(ContextKeys.AffectedBy, affectedBy);

        if (specFiles != null)
            context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, specFiles);

        context.Set(ContextKeys.DryRun, dryRun);

        return context;
    }

    /// <summary>
    /// Creates a DependencyGraph that will report the specified spec files as affected.
    /// </summary>
    private DraftSpec.Cli.DependencyGraph.DependencyGraph CreateGraphWithAffectedSpecs(params string[] affectedSpecFiles)
    {
        var graph = new DraftSpec.Cli.DependencyGraph.DependencyGraph(_pathComparer);

        // Add each spec file as a dependency with a namespace that matches what we'll change
        foreach (var specFile in affectedSpecFiles)
        {
            var dependency = new DraftSpec.Cli.DependencyGraph.SpecDependency(
                specFile,
                LoadDependencies: [],
                Namespaces: ["TestNamespace"]);
            graph.AddSpec(dependency);
        }

        // Map the changed source file to the TestNamespace
        graph.AddNamespaceMapping(TestPaths.Project("src/Service.cs"), "TestNamespace");

        return graph;
    }

    #endregion
}
