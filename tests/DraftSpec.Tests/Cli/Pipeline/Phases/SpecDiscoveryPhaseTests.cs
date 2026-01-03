using DraftSpec.Cli;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Common;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases;

/// <summary>
/// Tests for <see cref="SpecDiscoveryPhase"/>.
/// </summary>
public class SpecDiscoveryPhaseTests
{
    #region Happy Path Tests

    [Test]
    public async Task ExecuteAsync_SpecsFound_SetsSpecFiles()
    {
        var specFinder = new MockSpecFinder("/specs/a.spec.csx", "/specs/b.spec.csx");
        var phase = new SpecDiscoveryPhase(specFinder);
        var context = CreateContextWithProjectPath("/specs");
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(0); },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsTrue();

        var specFiles = context.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
        await Assert.That(specFiles).IsNotNull();
        await Assert.That(specFiles!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_SpecsFound_PropagatesPipelineResult()
    {
        var specFinder = new MockSpecFinder("/specs/test.spec.csx");
        var phase = new SpecDiscoveryPhase(specFinder);
        var context = CreateContextWithProjectPath("/specs");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    #endregion

    #region No Specs Tests

    [Test]
    public async Task ExecuteAsync_NoSpecsFound_ReturnsZero()
    {
        var specFinder = new MockSpecFinder();
        var phase = new SpecDiscoveryPhase(specFinder);
        var context = CreateContextWithProjectPath("/empty");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(99), // Should not be called
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_NoSpecsFound_WritesMessage()
    {
        var specFinder = new MockSpecFinder();
        var phase = new SpecDiscoveryPhase(specFinder);
        var console = new MockConsole();
        var context = CreateContextWithProjectPath("/empty", console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("No spec files found");
    }

    [Test]
    public async Task ExecuteAsync_NoSpecsFound_DoesNotCallPipeline()
    {
        var specFinder = new MockSpecFinder();
        var phase = new SpecDiscoveryPhase(specFinder);
        var context = CreateContextWithProjectPath("/empty");
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(0); },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsFalse();
    }

    #endregion

    #region Error Tests

    [Test]
    public async Task ExecuteAsync_ProjectPathNotSet_ReturnsError()
    {
        var specFinder = new MockSpecFinder();
        var phase = new SpecDiscoveryPhase(specFinder);
        var console = new MockConsole();
        var context = CreateContext(console); // No ProjectPath set

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("ProjectPath not set");
    }

    [Test]
    public async Task ExecuteAsync_FinderThrowsArgumentException_ReturnsError()
    {
        var specFinder = new ThrowingSpecFinder(new ArgumentException("Invalid path"));
        var phase = new SpecDiscoveryPhase(specFinder);
        var console = new MockConsole();
        var context = CreateContextWithProjectPath("/invalid", console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("Invalid path");
    }

    #endregion

    #region ExplicitFiles Tests

    [Test]
    public async Task ExecuteAsync_ExplicitFiles_UsesExplicitFilesInsteadOfFinder()
    {
        var specFinder = new MockSpecFinder("/other/should-not-be-called.spec.csx");
        var phase = new SpecDiscoveryPhase(specFinder);
        var console = new MockConsole();
        var fileSystem = new MockFileSystem()
            .AddFile("/project/a.spec.csx")
            .AddFile("/project/b.spec.csx");
        var context = CreateContextWithProjectPath("/project", console, fileSystem);
        context.Set<IReadOnlyList<string>>(ContextKeys.ExplicitFiles, new[] { "a.spec.csx", "b.spec.csx" });

        IReadOnlyList<string>? specFiles = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                specFiles = ctx.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(specFiles).IsNotNull();
        await Assert.That(specFiles!.Count).IsEqualTo(2);
        await Assert.That(specFiles).Contains("/project/a.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_ExplicitFiles_RelativePath_CombinesWithProjectPath()
    {
        var specFinder = new MockSpecFinder();
        var phase = new SpecDiscoveryPhase(specFinder);
        var console = new MockConsole();
        var fileSystem = new MockFileSystem()
            .AddFile("/project/specs/test.spec.csx");
        var context = CreateContextWithProjectPath("/project", console, fileSystem);
        context.Set<IReadOnlyList<string>>(ContextKeys.ExplicitFiles, new[] { "specs/test.spec.csx" });

        IReadOnlyList<string>? specFiles = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                specFiles = ctx.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(specFiles).IsNotNull();
        await Assert.That(specFiles![0]).IsEqualTo("/project/specs/test.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_ExplicitFiles_AbsolutePath_UsesAsIs()
    {
        var specFinder = new MockSpecFinder();
        var phase = new SpecDiscoveryPhase(specFinder);
        var console = new MockConsole();
        var fileSystem = new MockFileSystem()
            .AddFile("/absolute/path/test.spec.csx");
        var context = CreateContextWithProjectPath("/project", console, fileSystem);
        context.Set<IReadOnlyList<string>>(ContextKeys.ExplicitFiles, new[] { "/absolute/path/test.spec.csx" });

        IReadOnlyList<string>? specFiles = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                specFiles = ctx.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(specFiles).IsNotNull();
        await Assert.That(specFiles![0]).IsEqualTo("/absolute/path/test.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_ExplicitFiles_AllNonexistent_ReturnsZero()
    {
        var specFinder = new MockSpecFinder();
        var phase = new SpecDiscoveryPhase(specFinder);
        var console = new MockConsole();
        var fileSystem = new MockFileSystem(); // No files exist
        var context = CreateContextWithProjectPath("/project", console, fileSystem);
        context.Set<IReadOnlyList<string>>(ContextKeys.ExplicitFiles, new[] { "nonexistent.spec.csx" });

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(99), // Should not be called
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("No spec files found");
    }

    [Test]
    public async Task ExecuteAsync_ExplicitFiles_FiltersMissing_IncludesExisting()
    {
        var specFinder = new MockSpecFinder();
        var phase = new SpecDiscoveryPhase(specFinder);
        var console = new MockConsole();
        var fileSystem = new MockFileSystem()
            .AddFile("/project/exists.spec.csx");
        var context = CreateContextWithProjectPath("/project", console, fileSystem);
        context.Set<IReadOnlyList<string>>(ContextKeys.ExplicitFiles, new[] { "exists.spec.csx", "missing.spec.csx" });

        IReadOnlyList<string>? specFiles = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                specFiles = ctx.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(specFiles).IsNotNull();
        await Assert.That(specFiles!.Count).IsEqualTo(1);
        await Assert.That(specFiles[0]).IsEqualTo("/project/exists.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_EmptyExplicitFiles_FallsBackToFinder()
    {
        var specFinder = new MockSpecFinder("/project/discovered.spec.csx");
        var phase = new SpecDiscoveryPhase(specFinder);
        var console = new MockConsole();
        var context = CreateContextWithProjectPath("/project", console);
        context.Set<IReadOnlyList<string>>(ContextKeys.ExplicitFiles, Array.Empty<string>());

        IReadOnlyList<string>? specFiles = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                specFiles = ctx.Get<IReadOnlyList<string>>(ContextKeys.SpecFiles);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(specFiles).IsNotNull();
        await Assert.That(specFiles![0]).IsEqualTo("/project/discovered.spec.csx");
    }

    #endregion

    #region Helper Methods

    private static CommandContext CreateContext(MockConsole? console = null, MockFileSystem? fileSystem = null)
    {
        return new CommandContext
        {
            Path = "/test",
            Console = console ?? new MockConsole(),
            FileSystem = fileSystem ?? new MockFileSystem()
        };
    }

    private static CommandContext CreateContextWithProjectPath(
        string projectPath,
        MockConsole? console = null,
        MockFileSystem? fileSystem = null)
    {
        var context = CreateContext(console, fileSystem);
        context.Set<string>(ContextKeys.ProjectPath, projectPath);
        return context;
    }

    #endregion

    #region Helper Classes

    private class ThrowingSpecFinder : ISpecFinder
    {
        private readonly Exception _exception;

        public ThrowingSpecFinder(Exception exception)
        {
            _exception = exception;
        }

        public IReadOnlyList<string> FindSpecs(string path, string? baseDirectory = null)
        {
            throw _exception;
        }
    }

    #endregion
}
