using DraftSpec.Cli.Interactive;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Run;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Run;

/// <summary>
/// Tests for <see cref="InteractiveSelectionPhase"/>.
/// </summary>
public class InteractiveSelectionPhaseTests
{
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;
    private MockSpecSelector _selector = null!;
    private MockStaticSpecParserFactory _parserFactory = null!;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
        _selector = new MockSpecSelector();
        _parserFactory = new MockStaticSpecParserFactory();
    }

    #region Interactive Disabled Tests

    [Test]
    public async Task ExecuteAsync_InteractiveDisabled_PassesThroughUnchanged()
    {
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var context = CreateContext(interactive: false);
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
        await Assert.That(_selector.SelectAsyncCalls).IsEmpty();
    }

    [Test]
    public async Task ExecuteAsync_InteractiveDisabled_DoesNotModifyFilter()
    {
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var existingFilter = new FilterOptions { FilterName = "existing" };
        var context = CreateContext(interactive: false);
        context.Set(ContextKeys.Filter, existingFilter);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter).IsSameReferenceAs(existingFilter);
    }

    #endregion

    #region No Spec Files Tests

    [Test]
    public async Task ExecuteAsync_NoSpecFiles_ReturnsZero()
    {
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var context = CreateContext(interactive: true);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, []);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No spec files");
    }

    [Test]
    public async Task ExecuteAsync_NullSpecFiles_ReturnsZero()
    {
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var context = CreateContext(interactive: true);
        // Don't set SpecFiles

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No spec files");
    }

    #endregion

    #region No Specs Found Tests

    [Test]
    public async Task ExecuteAsync_NoSpecsFound_ReturnsZero()
    {
        _parserFactory.WithSpecCount(0);
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var context = CreateContext(interactive: true);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No specs found");
    }

    #endregion

    #region User Cancellation Tests

    [Test]
    public async Task ExecuteAsync_UserCancels_ReturnsZero()
    {
        _parserFactory.WithSpecCount(2);
        _selector.Cancelled();
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var context = CreateContext(interactive: true);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("Selection cancelled");
    }

    [Test]
    public async Task ExecuteAsync_UserCancels_DoesNotContinuePipeline()
    {
        _parserFactory.WithSpecCount(2);
        _selector.Cancelled();
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var context = CreateContext(interactive: true);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
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

    #region Selection Tests

    [Test]
    public async Task ExecuteAsync_UserSelectsSpecs_AddsFilterPattern()
    {
        _parserFactory.WithSpecCount(3);
        _selector.WithSelection("Context > spec1");
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var context = CreateContext(interactive: true);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter).IsNotNull();
        await Assert.That(filter!.FilterName).IsNotNull();
        await Assert.That(filter!.FilterName).Contains("Context");
        await Assert.That(filter!.FilterName).Contains("spec1");
    }

    [Test]
    public async Task ExecuteAsync_UserSelectsMultipleSpecs_BuildsOrPattern()
    {
        _parserFactory.WithSpecCount(3);
        _selector.WithSelection("Context > spec1", "Context > spec2");
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var context = CreateContext(interactive: true);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter!.FilterName).Contains("|");
    }

    [Test]
    public async Task ExecuteAsync_NoSpecsSelected_ReturnsZero()
    {
        _parserFactory.WithSpecCount(2);
        _selector.WithResult(SpecSelectionResult.Success([], [], 2));
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var context = CreateContext(interactive: true);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No specs selected");
    }

    #endregion

    #region Filter Merging Tests

    [Test]
    public async Task ExecuteAsync_ExistingFilter_MergesWithSelection()
    {
        _parserFactory.WithSpecCount(2);
        _selector.WithSelection("Context > spec1");
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var existingFilter = new FilterOptions { FilterName = "existing" };
        var context = CreateContext(interactive: true);
        context.Set(ContextKeys.Filter, existingFilter);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter!.FilterName).Contains("existing");
        await Assert.That(filter!.FilterName).Contains("spec1");
    }

    #endregion

    #region Cache Tests

    [Test]
    public async Task ExecuteAsync_NoCache_PassesUseCacheFalse()
    {
        _parserFactory.WithSpecCount(0);
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var context = CreateContext(interactive: true);
        context.Set(ContextKeys.NoCache, true);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_parserFactory.CreateCalls).Count().IsEqualTo(1);
        await Assert.That(_parserFactory.CreateCalls[0].UseCache).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_CacheEnabled_PassesUseCacheTrue()
    {
        _parserFactory.WithSpecCount(0);
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var context = CreateContext(interactive: true);
        context.Set(ContextKeys.NoCache, false);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_parserFactory.CreateCalls).Count().IsEqualTo(1);
        await Assert.That(_parserFactory.CreateCalls[0].UseCache).IsTrue();
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task ExecuteAsync_NoProjectPath_ReturnsError()
    {
        var phase = new InteractiveSelectionPhase(_selector, _parserFactory);
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.Interactive, true);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("ProjectPath not set");
    }

    #endregion

    #region Helper Methods

    private CommandContext CreateContext(bool interactive, string? projectPath = null)
    {
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.Interactive, interactive);
        context.Set(ContextKeys.ProjectPath, projectPath ?? TestPaths.ProjectDir);
        return context;
    }

    #endregion
}
