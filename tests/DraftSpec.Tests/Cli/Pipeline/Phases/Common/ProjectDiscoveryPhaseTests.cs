using DraftSpec.Cli;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Common;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Common;

/// <summary>
/// Tests for <see cref="ProjectDiscoveryPhase"/>.
/// </summary>
public class ProjectDiscoveryPhaseTests
{
    #region Happy Path Tests

    [Test]
    public async Task ExecuteAsync_ProjectFound_SetsProjectInfo()
    {
        var projectInfo = new ProjectInfo("/path/to/bin/Test.dll", "net10.0");
        var resolver = new MockProjectResolver
        {
            ProjectPath = "/path/to/Test.csproj",
            ProjectInfoResult = projectInfo
        };
        var phase = new ProjectDiscoveryPhase(resolver);
        var context = CreateContextWithProjectPath();

        ProjectInfo? result = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                result = ctx.Get<ProjectInfo>(ContextKeys.ProjectInfo);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TargetPath).IsEqualTo("/path/to/bin/Test.dll");
        await Assert.That(result.TargetFramework).IsEqualTo("net10.0");
    }

    [Test]
    public async Task ExecuteAsync_CallsPipeline()
    {
        var resolver = new MockProjectResolver { ProjectPath = "/path/to/Test.csproj" };
        var phase = new ProjectDiscoveryPhase(resolver);
        var context = CreateContextWithProjectPath();
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

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineResult()
    {
        var resolver = new MockProjectResolver { ProjectPath = "/path/to/Test.csproj" };
        var phase = new ProjectDiscoveryPhase(resolver);
        var context = CreateContextWithProjectPath();

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    #endregion

    #region No Project Found Tests

    [Test]
    public async Task ExecuteAsync_NoCsprojFound_SetsNullAndWarns()
    {
        var resolver = new MockProjectResolver { ProjectPath = null };
        var phase = new ProjectDiscoveryPhase(resolver);
        var console = new MockConsole();
        var context = CreateContextWithProjectPath(console);

        ProjectInfo? result = null;
        var wasCalled = false;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                wasCalled = true;
                result = ctx.Get<ProjectInfo>(ContextKeys.ProjectInfo);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(wasCalled).IsTrue();
        await Assert.That(result).IsNull();
        await Assert.That(console.Warnings).Contains("No .csproj found");
    }

    [Test]
    public async Task ExecuteAsync_ProjectInfoNull_SetsNullAndWarns()
    {
        var resolver = new MockProjectResolver
        {
            ProjectPath = "/path/to/Test.csproj",
            ProjectInfoResult = null
        };
        var phase = new ProjectDiscoveryPhase(resolver);
        var console = new MockConsole();
        var context = CreateContextWithProjectPath(console);

        ProjectInfo? result = null;
        var wasCalled = false;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                wasCalled = true;
                result = ctx.Get<ProjectInfo>(ContextKeys.ProjectInfo);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(wasCalled).IsTrue();
        await Assert.That(result).IsNull();
        await Assert.That(console.Warnings).Contains("Could not get project info");
    }

    #endregion

    #region Precondition Tests

    [Test]
    public async Task ExecuteAsync_ProjectPathNotSet_ReturnsError()
    {
        var resolver = new MockProjectResolver();
        var phase = new ProjectDiscoveryPhase(resolver);
        var console = new MockConsole();
        var context = CreateContext(console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("ProjectPath not set");
    }

    [Test]
    public async Task ExecuteAsync_ProjectPathNotSet_DoesNotCallPipeline()
    {
        var resolver = new MockProjectResolver();
        var phase = new ProjectDiscoveryPhase(resolver);
        var context = CreateContext();
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsFalse();
    }

    #endregion

    #region Helper Methods

    private static CommandContext CreateContext(MockConsole? console = null)
    {
        return new CommandContext
        {
            Path = TestPaths.ProjectDir,
            Console = console ?? new MockConsole(),
            FileSystem = new MockFileSystem()
        };
    }

    private static CommandContext CreateContextWithProjectPath(MockConsole? console = null)
    {
        var context = CreateContext(console);
        context.Set(ContextKeys.ProjectPath, TestPaths.ProjectDir);
        return context;
    }

    #endregion
}
