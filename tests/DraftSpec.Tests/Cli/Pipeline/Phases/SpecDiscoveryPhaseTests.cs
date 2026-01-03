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

    #region Helper Methods

    private static CommandContext CreateContext(MockConsole? console = null)
    {
        return new CommandContext
        {
            Path = "/test",
            Console = console ?? new MockConsole(),
            FileSystem = new MockFileSystem()
        };
    }

    private static CommandContext CreateContextWithProjectPath(string projectPath, MockConsole? console = null)
    {
        var context = CreateContext(console);
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
