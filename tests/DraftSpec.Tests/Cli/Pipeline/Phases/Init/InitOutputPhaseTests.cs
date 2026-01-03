using DraftSpec.Cli;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Init;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Init;

/// <summary>
/// Tests for <see cref="InitOutputPhase"/>.
/// </summary>
public class InitOutputPhaseTests
{
    #region File Creation Tests

    [Test]
    public async Task ExecuteAsync_CreatesSpecHelper()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = new InitOutputPhase();
        var context = CreateContextWithProjectPath(fileSystem);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var specHelperPath = Path.Combine(TestPaths.ProjectDir, "spec_helper.csx");
        await Assert.That(fileSystem.WrittenFiles.ContainsKey(specHelperPath)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_CreatesOmnisharp()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = new InitOutputPhase();
        var context = CreateContextWithProjectPath(fileSystem);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var omnisharpPath = Path.Combine(TestPaths.ProjectDir, "omnisharp.json");
        await Assert.That(fileSystem.WrittenFiles.ContainsKey(omnisharpPath)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SpecHelperContainsDraftSpecReference()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = new InitOutputPhase();
        var context = CreateContextWithProjectPath(fileSystem);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var specHelperPath = Path.Combine(TestPaths.ProjectDir, "spec_helper.csx");
        var content = fileSystem.WrittenFiles[specHelperPath];
        await Assert.That(content).Contains("#r \"nuget: DraftSpec, *\"");
        await Assert.That(content).Contains("using static DraftSpec.Dsl;");
    }

    [Test]
    public async Task ExecuteAsync_OmnisharpContainsScriptConfig()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = new InitOutputPhase();
        var context = CreateContextWithProjectPath(fileSystem);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var omnisharpPath = Path.Combine(TestPaths.ProjectDir, "omnisharp.json");
        var content = fileSystem.WrittenFiles[omnisharpPath];
        await Assert.That(content).Contains("enableScriptNuGetReferences");
        await Assert.That(content).Contains("defaultTargetFramework");
    }

    [Test]
    public async Task ExecuteAsync_WithProjectInfo_AddsProjectReference()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = new InitOutputPhase();
        var context = CreateContextWithProjectPath(fileSystem);
        context.Set(ContextKeys.ProjectInfo, new ProjectInfo(
            Path.Combine(TestPaths.ProjectDir, "bin", "Debug", "net10.0", "MyProject.dll"),
            "net10.0"));

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var specHelperPath = Path.Combine(TestPaths.ProjectDir, "spec_helper.csx");
        var content = fileSystem.WrittenFiles[specHelperPath];
        await Assert.That(content).Contains("#r \"bin");
        await Assert.That(content).Contains("MyProject.dll");
    }

    [Test]
    public async Task ExecuteAsync_WithProjectInfo_UsesTargetFramework()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = new InitOutputPhase();
        var context = CreateContextWithProjectPath(fileSystem);
        context.Set(ContextKeys.ProjectInfo, new ProjectInfo(
            Path.Combine(TestPaths.ProjectDir, "bin", "MyProject.dll"),
            "net9.0"));

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var omnisharpPath = Path.Combine(TestPaths.ProjectDir, "omnisharp.json");
        var content = fileSystem.WrittenFiles[omnisharpPath];
        await Assert.That(content).Contains("net9.0");
    }

    #endregion

    #region Existing Files Tests

    [Test]
    public async Task ExecuteAsync_ExistingSpecHelper_DoesNotOverwrite()
    {
        var specHelperPath = Path.Combine(TestPaths.ProjectDir, "spec_helper.csx");
        var fileSystem = new MockFileSystem()
            .AddDirectory(TestPaths.ProjectDir)
            .AddFile(specHelperPath, "// original content");
        var phase = new InitOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithProjectPath(fileSystem, console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(fileSystem.WrittenFiles[specHelperPath]).IsEqualTo("// original content");
        await Assert.That(console.Output).Contains("already exists");
    }

    [Test]
    public async Task ExecuteAsync_ExistingSpecHelper_WithForce_Overwrites()
    {
        var specHelperPath = Path.Combine(TestPaths.ProjectDir, "spec_helper.csx");
        var fileSystem = new MockFileSystem()
            .AddDirectory(TestPaths.ProjectDir)
            .AddFile(specHelperPath, "// original content");
        var phase = new InitOutputPhase();
        var context = CreateContextWithProjectPath(fileSystem);
        context.Set(ContextKeys.Force, true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(fileSystem.WrittenFiles[specHelperPath]).Contains("#r \"nuget: DraftSpec, *\"");
    }

    [Test]
    public async Task ExecuteAsync_ExistingOmnisharp_DoesNotOverwrite()
    {
        var omnisharpPath = Path.Combine(TestPaths.ProjectDir, "omnisharp.json");
        var fileSystem = new MockFileSystem()
            .AddDirectory(TestPaths.ProjectDir)
            .AddFile(omnisharpPath, "{ \"original\": true }");
        var phase = new InitOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithProjectPath(fileSystem, console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(fileSystem.WrittenFiles[omnisharpPath]).IsEqualTo("{ \"original\": true }");
    }

    [Test]
    public async Task ExecuteAsync_ExistingOmnisharp_WithForce_Overwrites()
    {
        var omnisharpPath = Path.Combine(TestPaths.ProjectDir, "omnisharp.json");
        var fileSystem = new MockFileSystem()
            .AddDirectory(TestPaths.ProjectDir)
            .AddFile(omnisharpPath, "{ \"original\": true }");
        var phase = new InitOutputPhase();
        var context = CreateContextWithProjectPath(fileSystem);
        context.Set(ContextKeys.Force, true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(fileSystem.WrittenFiles[omnisharpPath]).Contains("enableScriptNuGetReferences");
    }

    #endregion

    #region Console Output Tests

    [Test]
    public async Task ExecuteAsync_Success_ShowsSuccessMessages()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = new InitOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithProjectPath(fileSystem, console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("Created spec_helper.csx");
        await Assert.That(console.Output).Contains("Created omnisharp.json");
    }

    #endregion

    #region Pipeline Propagation Tests

    [Test]
    public async Task ExecuteAsync_CallsPipeline()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = new InitOutputPhase();
        var context = CreateContextWithProjectPath(fileSystem);
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineResult()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = new InitOutputPhase();
        var context = CreateContextWithProjectPath(fileSystem);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    #endregion

    #region Precondition Tests

    [Test]
    public async Task ExecuteAsync_ProjectPathNotSet_ReturnsError()
    {
        var phase = new InitOutputPhase();
        var console = new MockConsole();
        var context = CreateContext(console: console);

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
        var phase = new InitOutputPhase();
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

    private static CommandContext CreateContext(MockFileSystem? fileSystem = null, MockConsole? console = null)
    {
        return new CommandContext
        {
            Path = TestPaths.ProjectDir,
            Console = console ?? new MockConsole(),
            FileSystem = fileSystem ?? new MockFileSystem()
        };
    }

    private static CommandContext CreateContextWithProjectPath(MockFileSystem? fileSystem = null, MockConsole? console = null)
    {
        var context = CreateContext(fileSystem, console);
        context.Set(ContextKeys.ProjectPath, TestPaths.ProjectDir);
        return context;
    }

    #endregion
}
