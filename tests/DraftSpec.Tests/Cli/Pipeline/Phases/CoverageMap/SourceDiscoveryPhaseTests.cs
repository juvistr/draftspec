using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.CoverageMap;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.CoverageMap;

/// <summary>
/// Tests for <see cref="SourceDiscoveryPhase"/>.
/// </summary>
public class SourceDiscoveryPhaseTests
{
    #region Happy Path Tests

    [Test]
    public async Task ExecuteAsync_SourceFilesFound_SetsSourceFiles()
    {
        var fileSystem = new MockFileSystem()
            .AddFile(TestPaths.Project("src/Service.cs"))
            .AddDirectory(TestPaths.Project("src"));
        var phase = new SourceDiscoveryPhase(fileSystem);
        var context = CreateContextWithProjectPath(TestPaths.ProjectDir, fileSystem);
        context.Set(ContextKeys.SourcePath, TestPaths.Project("src"));
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(0); },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsTrue();

        var sourceFiles = context.Get<IReadOnlyList<string>>(ContextKeys.SourceFiles);
        await Assert.That(sourceFiles).IsNotNull();
        await Assert.That(sourceFiles!.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineResult()
    {
        var fileSystem = new MockFileSystem()
            .AddFile(TestPaths.Project("Service.cs"))
            .AddDirectory(TestPaths.ProjectDir);
        var phase = new SourceDiscoveryPhase(fileSystem);
        var context = CreateContextWithProjectPath(TestPaths.ProjectDir, fileSystem);
        context.Set(ContextKeys.SourcePath, TestPaths.ProjectDir);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task ExecuteAsync_SourcePathNotSet_DefaultsToProjectPath()
    {
        var fileSystem = new MockFileSystem()
            .AddFile(TestPaths.Project("Service.cs"))
            .AddDirectory(TestPaths.ProjectDir);
        var phase = new SourceDiscoveryPhase(fileSystem);
        var context = CreateContextWithProjectPath(TestPaths.ProjectDir, fileSystem);
        // Don't set SourcePath - should default to ProjectPath

        IReadOnlyList<string>? sourceFiles = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                sourceFiles = ctx.Get<IReadOnlyList<string>>(ContextKeys.SourceFiles);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(sourceFiles).IsNotNull();
        await Assert.That(sourceFiles!.Count).IsEqualTo(1);
    }

    #endregion

    #region No Source Files Tests

    [Test]
    public async Task ExecuteAsync_NoSourceFiles_ReturnsError()
    {
        var fileSystem = new MockFileSystem()
            .AddDirectory(TestPaths.ProjectDir);
        var phase = new SourceDiscoveryPhase(fileSystem);
        var console = new MockConsole();
        var context = CreateContextWithProjectPath(TestPaths.ProjectDir, fileSystem, console);
        context.Set(ContextKeys.SourcePath, TestPaths.ProjectDir);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(99),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("No C# source files found");
    }

    [Test]
    public async Task ExecuteAsync_NoSourceFiles_DoesNotCallPipeline()
    {
        var fileSystem = new MockFileSystem()
            .AddDirectory(TestPaths.ProjectDir);
        var phase = new SourceDiscoveryPhase(fileSystem);
        var context = CreateContextWithProjectPath(TestPaths.ProjectDir, fileSystem);
        context.Set(ContextKeys.SourcePath, TestPaths.ProjectDir);
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(0); },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsFalse();
    }

    #endregion

    #region Precondition Tests

    [Test]
    public async Task ExecuteAsync_ProjectPathNotSet_ReturnsError()
    {
        var fileSystem = new MockFileSystem();
        var phase = new SourceDiscoveryPhase(fileSystem);
        var console = new MockConsole();
        var context = CreateContext(console, fileSystem);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("ProjectPath not set");
    }

    [Test]
    public async Task ExecuteAsync_SourcePathNotFound_ReturnsError()
    {
        var fileSystem = new MockFileSystem();
        var phase = new SourceDiscoveryPhase(fileSystem);
        var console = new MockConsole();
        var context = CreateContextWithProjectPath(TestPaths.ProjectDir, fileSystem, console);
        context.Set(ContextKeys.SourcePath, "/nonexistent/path");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("Source path not found");
    }

    #endregion

    #region Generated Files Tests

    [Test]
    public async Task ExecuteAsync_SkipsGeneratedFiles()
    {
        var fileSystem = new MockFileSystem()
            .AddFile(TestPaths.Project("Service.cs"))
            .AddFile(TestPaths.Project("Service.g.cs"))
            .AddFile(TestPaths.Project("Service.generated.cs"))
            .AddFile(TestPaths.Project("Service.designer.cs"))
            .AddDirectory(TestPaths.ProjectDir);
        var phase = new SourceDiscoveryPhase(fileSystem);
        var context = CreateContextWithProjectPath(TestPaths.ProjectDir, fileSystem);
        context.Set(ContextKeys.SourcePath, TestPaths.ProjectDir);

        IReadOnlyList<string>? sourceFiles = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                sourceFiles = ctx.Get<IReadOnlyList<string>>(ContextKeys.SourceFiles);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(sourceFiles).Count().IsEqualTo(1);
        await Assert.That(sourceFiles![0]).Contains("Service.cs");
        await Assert.That(sourceFiles[0]).DoesNotContain(".g.cs");
    }

    #endregion

    #region Single File Tests

    [Test]
    public async Task ExecuteAsync_SingleCsFile_ReturnsThatFile()
    {
        var filePath = TestPaths.Project("SingleService.cs");
        var fileSystem = new MockFileSystem()
            .AddFile(filePath);
        var phase = new SourceDiscoveryPhase(fileSystem);
        var context = CreateContextWithProjectPath(TestPaths.ProjectDir, fileSystem);
        context.Set(ContextKeys.SourcePath, filePath);

        IReadOnlyList<string>? sourceFiles = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                sourceFiles = ctx.Get<IReadOnlyList<string>>(ContextKeys.SourceFiles);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(sourceFiles).IsNotNull();
        await Assert.That(sourceFiles!.Count).IsEqualTo(1);
        await Assert.That(sourceFiles[0]).IsEqualTo(filePath);
    }

    #endregion

    #region Helper Methods

    private static CommandContext CreateContext(MockConsole? console = null, MockFileSystem? fileSystem = null)
    {
        return new CommandContext
        {
            Path = TestPaths.ProjectDir,
            Console = console ?? new MockConsole(),
            FileSystem = fileSystem ?? new MockFileSystem()
        };
    }

    private static CommandContext CreateContextWithProjectPath(
        string projectPath,
        MockFileSystem? fileSystem = null,
        MockConsole? console = null)
    {
        var context = CreateContext(console, fileSystem);
        context.Set(ContextKeys.ProjectPath, projectPath);
        return context;
    }

    #endregion
}
