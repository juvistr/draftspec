using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Run;
using DraftSpec.TestingPlatform;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Run;

/// <summary>
/// Tests for <see cref="LineFilterPhase"/>.
/// </summary>
public class LineFilterPhaseTests
{
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;
    private MockStaticSpecParser _mockParser = null!;
    private MockStaticSpecParserFactory _parserFactory = null!;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
        _mockParser = new MockStaticSpecParser();
        _parserFactory = new MockStaticSpecParserFactory(_mockParser);
    }

    #region No Line Filters Tests

    [Test]
    public async Task ExecuteAsync_NoLineFilters_PassesThroughUnchanged()
    {
        var phase = new LineFilterPhase(_parserFactory);
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
        await Assert.That(_parserFactory.CreateCalls).IsEmpty();
    }

    [Test]
    public async Task ExecuteAsync_EmptyLineFilters_PassesThroughUnchanged()
    {
        var phase = new LineFilterPhase(_parserFactory);
        var context = CreateContext();
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, []);
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

    #region File Not Found Tests

    [Test]
    public async Task ExecuteAsync_FileNotFound_ShowsWarning()
    {
        var phase = new LineFilterPhase(_parserFactory);
        var context = CreateContext();
        var lineFilters = new List<LineFilter> { new("missing.spec.csx", [10]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Warnings).Contains("File not found: missing.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_FileNotFound_NoSpecsAtLines_ReturnsError()
    {
        var phase = new LineFilterPhase(_parserFactory);
        var context = CreateContext();
        var lineFilters = new List<LineFilter> { new("missing.spec.csx", [10]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("No specs found at the specified line numbers");
    }

    #endregion

    #region Spec Matching Tests

    [Test]
    public async Task ExecuteAsync_LineMatchesSpec_AddsFilterPattern()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _fileSystem.AddFile(specFile, "");
        _mockParser.WithSpecs(specFile,
            new StaticSpec
            {
                Description = "should work",
                ContextPath = ["Service"],
                LineNumber = 10,
                Type = StaticSpecType.Regular
            });

        var phase = new LineFilterPhase(_parserFactory);
        var context = CreateContext();
        var lineFilters = new List<LineFilter> { new("specs/test.spec.csx", [10]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter).IsNotNull();
        // Regex.Escape escapes spaces
        await Assert.That(filter!.FilterName).Contains("Service");
        await Assert.That(filter!.FilterName).Contains("should");
        await Assert.That(filter!.FilterName).Contains("work");
    }

    [Test]
    public async Task ExecuteAsync_LineNearSpec_IncludesNearbySpec()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _fileSystem.AddFile(specFile, "");
        _mockParser.WithSpecs(specFile,
            new StaticSpec
            {
                Description = "should work",
                ContextPath = ["Service"],
                LineNumber = 10,
                Type = StaticSpecType.Regular
            });

        var phase = new LineFilterPhase(_parserFactory);
        var context = CreateContext();
        // Line 11 is within 1 line of spec at line 10
        var lineFilters = new List<LineFilter> { new("specs/test.spec.csx", [11]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter).IsNotNull();
        // Regex.Escape escapes spaces
        await Assert.That(filter!.FilterName).Contains("Service");
        await Assert.That(filter!.FilterName).Contains("should");
        await Assert.That(filter!.FilterName).Contains("work");
    }

    [Test]
    public async Task ExecuteAsync_MultipleSpecs_BuildsOrPattern()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _fileSystem.AddFile(specFile, "");
        _mockParser.WithSpecs(specFile,
            new StaticSpec
            {
                Description = "spec one",
                ContextPath = ["Context"],
                LineNumber = 10,
                Type = StaticSpecType.Regular
            },
            new StaticSpec
            {
                Description = "spec two",
                ContextPath = ["Context"],
                LineNumber = 20,
                Type = StaticSpecType.Regular
            });

        var phase = new LineFilterPhase(_parserFactory);
        var context = CreateContext();
        var lineFilters = new List<LineFilter> { new("specs/test.spec.csx", [10, 20]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        // Regex.Escape escapes spaces
        await Assert.That(filter!.FilterName).Contains("spec");
        await Assert.That(filter!.FilterName).Contains("one");
        await Assert.That(filter!.FilterName).Contains("two");
        await Assert.That(filter!.FilterName).Contains("|");
    }

    [Test]
    public async Task ExecuteAsync_NoSpecsAtLines_ReturnsError()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _fileSystem.AddFile(specFile, "");
        _mockParser.WithSpecs(specFile,
            new StaticSpec
            {
                Description = "spec at line 50",
                ContextPath = ["Context"],
                LineNumber = 50,
                Type = StaticSpecType.Regular
            });

        var phase = new LineFilterPhase(_parserFactory);
        var context = CreateContext();
        // Line 10 is far from spec at line 50
        var lineFilters = new List<LineFilter> { new("specs/test.spec.csx", [10]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("No specs found at the specified line numbers");
    }

    #endregion

    #region Filter Merging Tests

    [Test]
    public async Task ExecuteAsync_ExistingFilterName_MergesPatterns()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _fileSystem.AddFile(specFile, "");
        _mockParser.WithSpecs(specFile,
            new StaticSpec
            {
                Description = "new spec",
                ContextPath = ["Context"],
                LineNumber = 10,
                Type = StaticSpecType.Regular
            });

        var phase = new LineFilterPhase(_parserFactory);
        var existingFilter = new FilterOptions { FilterName = "existing pattern" };
        var context = CreateContext();
        context.Set(ContextKeys.Filter, existingFilter);
        var lineFilters = new List<LineFilter> { new("specs/test.spec.csx", [10]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter!.FilterName).Contains("existing pattern");
        // Regex.Escape escapes spaces
        await Assert.That(filter!.FilterName).Contains("new");
        await Assert.That(filter!.FilterName).Contains("spec");
    }

    [Test]
    public async Task ExecuteAsync_NoExistingFilter_CreatesNewFilter()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _fileSystem.AddFile(specFile, "");
        _mockParser.WithSpecs(specFile,
            new StaticSpec
            {
                Description = "spec",
                ContextPath = ["Context"],
                LineNumber = 10,
                Type = StaticSpecType.Regular
            });

        var phase = new LineFilterPhase(_parserFactory);
        var context = CreateContext();
        var lineFilters = new List<LineFilter> { new("specs/test.spec.csx", [10]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter).IsNotNull();
        await Assert.That(filter!.FilterName).StartsWith("^(");
        await Assert.That(filter!.FilterName).EndsWith(")$");
    }

    #endregion

    #region Parser Configuration Tests

    [Test]
    public async Task ExecuteAsync_UsesCorrectCacheSetting()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _fileSystem.AddFile(specFile, "");
        _mockParser.WithSpecs(specFile,
            new StaticSpec
            {
                Description = "spec",
                ContextPath = ["Context"],
                LineNumber = 10,
                Type = StaticSpecType.Regular
            });

        var phase = new LineFilterPhase(_parserFactory);
        var context = CreateContext();
        context.Set(ContextKeys.NoCache, true);
        var lineFilters = new List<LineFilter> { new("specs/test.spec.csx", [10]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_parserFactory.CreateCalls).Count().IsEqualTo(1);
        await Assert.That(_parserFactory.CreateCalls[0].UseCache).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_CacheEnabled_ByDefault()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _fileSystem.AddFile(specFile, "");
        _mockParser.WithSpecs(specFile,
            new StaticSpec
            {
                Description = "spec",
                ContextPath = ["Context"],
                LineNumber = 10,
                Type = StaticSpecType.Regular
            });

        var phase = new LineFilterPhase(_parserFactory);
        var context = CreateContext();
        var lineFilters = new List<LineFilter> { new("specs/test.spec.csx", [10]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_parserFactory.CreateCalls[0].UseCache).IsTrue();
    }

    #endregion

    #region Display Name Generation Tests

    [Test]
    public async Task ExecuteAsync_EmptyContextPath_UsesDescriptionOnly()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _fileSystem.AddFile(specFile, "");
        _mockParser.WithSpecs(specFile,
            new StaticSpec
            {
                Description = "standalone spec",
                ContextPath = [],
                LineNumber = 10,
                Type = StaticSpecType.Regular
            });

        var phase = new LineFilterPhase(_parserFactory);
        var context = CreateContext();
        var lineFilters = new List<LineFilter> { new("specs/test.spec.csx", [10]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        // Regex.Escape escapes spaces
        await Assert.That(filter!.FilterName).Contains("standalone");
        await Assert.That(filter!.FilterName).Contains("spec");
        await Assert.That(filter!.FilterName).DoesNotContain(">");
    }

    [Test]
    public async Task ExecuteAsync_NestedContextPath_FormatsCorrectly()
    {
        var specFile = TestPaths.Project("specs/test.spec.csx");
        _fileSystem.AddFile(specFile, "");
        _mockParser.WithSpecs(specFile,
            new StaticSpec
            {
                Description = "should work",
                ContextPath = ["Service", "Method", "Scenario"],
                LineNumber = 10,
                Type = StaticSpecType.Regular
            });

        var phase = new LineFilterPhase(_parserFactory);
        var context = CreateContext();
        var lineFilters = new List<LineFilter> { new("specs/test.spec.csx", [10]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        // Regex.Escape escapes spaces and > remains unescaped
        await Assert.That(filter!.FilterName).Contains("Service");
        await Assert.That(filter!.FilterName).Contains("Method");
        await Assert.That(filter!.FilterName).Contains("Scenario");
        await Assert.That(filter!.FilterName).Contains("should");
        await Assert.That(filter!.FilterName).Contains("work");
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task ExecuteAsync_NoProjectPath_ReturnsError()
    {
        var phase = new LineFilterPhase(_parserFactory);
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        var lineFilters = new List<LineFilter> { new("test.spec.csx", [10]) };
        context.Set<IReadOnlyList<LineFilter>>(ContextKeys.LineFilters, lineFilters);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("ProjectPath not set");
    }

    #endregion

    #region Helper Methods

    private CommandContext CreateContext(string? projectPath = null)
    {
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.ProjectPath, projectPath ?? TestPaths.ProjectDir);
        return context;
    }

    #endregion
}
