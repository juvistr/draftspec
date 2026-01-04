using DraftSpec.Abstractions;
using DraftSpec.Cli;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.NewSpec;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.NewSpec;

/// <summary>
/// Tests for <see cref="NewSpecOutputPhase"/>.
/// </summary>
public class NewSpecOutputPhaseTests
{
    private static NewSpecOutputPhase CreatePhase()
    {
        var pathValidator = new PathValidator(new SystemPathComparer(new SystemOperatingSystem()));
        return new NewSpecOutputPhase(pathValidator);
    }

    #region File Creation Tests

    [Test]
    public async Task ExecuteAsync_ValidName_CreatesSpecFile()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, "MyFeature");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var expectedPath = Path.Combine(TestPaths.ProjectDir, "MyFeature.spec.csx");
        await Assert.That(fileSystem.WrittenFiles.ContainsKey(expectedPath)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_ValidName_SpecFileHasCorrectContent()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, "UserService");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var specPath = Path.Combine(TestPaths.ProjectDir, "UserService.spec.csx");
        var content = fileSystem.WrittenFiles[specPath];
        await Assert.That(content).Contains("#load \"spec_helper.csx\"");
        await Assert.That(content).Contains("using static DraftSpec.Dsl;");
        await Assert.That(content).Contains("describe(\"UserService\"");
    }

    [Test]
    public async Task ExecuteAsync_Success_ShowsCreatedMessage()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var console = new MockConsole();
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, "MySpec", console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("Created MySpec.spec.csx");
    }

    #endregion

    #region Existing File Tests

    [Test]
    public async Task ExecuteAsync_ExistingSpec_ReturnsError()
    {
        var specPath = Path.Combine(TestPaths.ProjectDir, "Existing.spec.csx");
        var fileSystem = new MockFileSystem()
            .AddDirectory(TestPaths.ProjectDir)
            .AddFile(specPath, "// existing");
        var console = new MockConsole();
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, "Existing", console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("already exists");
    }

    [Test]
    public async Task ExecuteAsync_ExistingSpec_DoesNotOverwrite()
    {
        var specPath = Path.Combine(TestPaths.ProjectDir, "Existing.spec.csx");
        var fileSystem = new MockFileSystem()
            .AddDirectory(TestPaths.ProjectDir)
            .AddFile(specPath, "// original content");
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, "Existing");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(fileSystem.WrittenFiles[specPath]).IsEqualTo("// original content");
    }

    #endregion

    #region Validation Tests

    [Test]
    public async Task ExecuteAsync_MissingName_ReturnsError()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var console = new MockConsole();
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, null, console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("Usage:");
    }

    [Test]
    public async Task ExecuteAsync_EmptyName_ReturnsError()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var console = new MockConsole();
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, "", console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_NameWithPathSeparator_ReturnsError()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var console = new MockConsole();
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, "foo/bar", console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("path separator");
    }

    [Test]
    public async Task ExecuteAsync_NameWithDoubleDot_ReturnsError()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var console = new MockConsole();
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, "..", console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("relative path reference");
    }

    [Test]
    public async Task ExecuteAsync_NameWithInvalidChars_ReturnsError()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var console = new MockConsole();
        var phase = CreatePhase();
        // Use a character that's invalid on all platforms (null char)
        var context = CreateContextWithProjectPath(fileSystem, "test\0name", console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("invalid characters");
    }

    #endregion

    #region Warning Tests

    [Test]
    public async Task ExecuteAsync_NoSpecHelper_ShowsWarning()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var console = new MockConsole();
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, "MySpec", console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("spec_helper.csx not found");
    }

    [Test]
    public async Task ExecuteAsync_WithSpecHelper_NoWarning()
    {
        var specHelperPath = Path.Combine(TestPaths.ProjectDir, "spec_helper.csx");
        var fileSystem = new MockFileSystem()
            .AddDirectory(TestPaths.ProjectDir)
            .AddFile(specHelperPath, "// helper");
        var console = new MockConsole();
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, "MySpec", console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).DoesNotContain("spec_helper.csx not found");
    }

    #endregion

    #region Precondition Tests

    [Test]
    public async Task ExecuteAsync_ProjectPathNotSet_ReturnsError()
    {
        var phase = CreatePhase();
        var console = new MockConsole();
        var context = CreateContext(console: console);
        context.Set(ContextKeys.SpecName, "Test");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("ProjectPath not set");
    }

    #endregion

    #region Pipeline Propagation Tests

    [Test]
    public async Task ExecuteAsync_CallsPipeline()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, "Test");
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
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, "Test");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task ExecuteAsync_Error_DoesNotCallPipeline()
    {
        var fileSystem = new MockFileSystem().AddDirectory(TestPaths.ProjectDir);
        var phase = CreatePhase();
        var context = CreateContextWithProjectPath(fileSystem, null); // Invalid name
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

    private static CommandContext CreateContextWithProjectPath(
        MockFileSystem? fileSystem = null,
        string? specName = null,
        MockConsole? console = null)
    {
        var context = CreateContext(fileSystem, console);
        context.Set(ContextKeys.ProjectPath, TestPaths.ProjectDir);
        context.Set(ContextKeys.SpecName, specName);
        return context;
    }

    #endregion
}
