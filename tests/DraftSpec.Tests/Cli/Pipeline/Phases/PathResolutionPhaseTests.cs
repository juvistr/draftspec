using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Common;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases;

/// <summary>
/// Tests for <see cref="PathResolutionPhase"/>.
/// </summary>
public class PathResolutionPhaseTests
{
    #region Happy Path Tests

    [Test]
    public async Task ExecuteAsync_DirectoryExists_SetsProjectPath()
    {
        var phase = new PathResolutionPhase();
        var specsDir = TestPaths.SpecsDir;
        var fs = new MockFileSystem().AddDirectory(specsDir);
        var context = CreateContext(specsDir, fs);
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(0); },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsTrue();
        await Assert.That(context.Get<string>(ContextKeys.ProjectPath)).IsEqualTo(specsDir);
    }

    [Test]
    public async Task ExecuteAsync_FileExists_SetsProjectPathToDirectory()
    {
        var phase = new PathResolutionPhase();
        var specFile = TestPaths.Spec("test.spec.csx");
        var fs = new MockFileSystem().AddFile(specFile);
        var context = CreateContext(specFile, fs);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(context.Get<string>(ContextKeys.ProjectPath)).IsEqualTo(TestPaths.SpecsDir);
    }

    [Test]
    public async Task ExecuteAsync_RelativePath_ResolvesToAbsolute()
    {
        var phase = new PathResolutionPhase();
        var absolutePath = Path.GetFullPath("specs");
        var fs = new MockFileSystem().AddDirectory(absolutePath);
        var context = CreateContext("specs", fs);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(context.Get<string>(ContextKeys.ProjectPath)).IsEqualTo(absolutePath);
    }

    #endregion

    #region Error Tests

    [Test]
    public async Task ExecuteAsync_PathNotFound_ReturnsErrorAndWritesError()
    {
        var phase = new PathResolutionPhase();
        var console = new MockConsole();
        var fs = new MockFileSystem(); // No files/directories exist
        var context = CreateContext("/nonexistent", fs, console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("Path not found");
    }

    [Test]
    public async Task ExecuteAsync_PathNotFound_DoesNotCallPipeline()
    {
        var phase = new PathResolutionPhase();
        var fs = new MockFileSystem();
        var context = CreateContext("/nonexistent", fs);
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(0); },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsFalse();
    }

    #endregion

    #region Pipeline Propagation Tests

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue()
    {
        var phase = new PathResolutionPhase();
        var specsDir = TestPaths.SpecsDir;
        var fs = new MockFileSystem().AddDirectory(specsDir);
        var context = CreateContext(specsDir, fs);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    #endregion

    #region Helper Methods

    private static CommandContext CreateContext(string path, MockFileSystem? fs = null, MockConsole? console = null)
    {
        return new CommandContext
        {
            Path = path,
            Console = console ?? new MockConsole(),
            FileSystem = fs ?? new MockFileSystem()
        };
    }

    #endregion
}
