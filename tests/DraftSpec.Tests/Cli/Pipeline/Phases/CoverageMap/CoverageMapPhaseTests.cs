using DraftSpec.Cli.CoverageMap;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.CoverageMap;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.CoverageMap;

/// <summary>
/// Tests for <see cref="CoverageMapPhase"/>.
/// </summary>
public class CoverageMapPhaseTests
{
    #region Happy Path Tests

    [Test]
    public async Task ExecuteAsync_ValidInputs_ComputesCoverage()
    {
        var coverageService = new MockCoverageMapService()
            .WithMethods(CreateMethodCoverage("CreateUser", CoverageConfidence.High));
        var specFinder = new MockSpecFinder(TestPaths.Project("test.spec.csx"));
        var phase = new CoverageMapPhase(coverageService, specFinder);
        var context = CreateContextWithSourceFiles();

        CoverageMapResult? result = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                result = ctx.Get<CoverageMapResult>(ContextKeys.CoverageMapResult);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.AllMethods.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_CallsServiceWithCorrectParameters()
    {
        var coverageService = new MockCoverageMapService();
        var specFinder = new MockSpecFinder(TestPaths.Project("test.spec.csx"));
        var phase = new CoverageMapPhase(coverageService, specFinder);
        var context = CreateContextWithSourceFiles();
        context.Set(ContextKeys.NamespaceFilter, "MyApp.Services");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(coverageService.ComputeCoverageAsyncCalls).Count().IsEqualTo(1);
        var call = coverageService.ComputeCoverageAsyncCalls[0];
        await Assert.That(call.NamespaceFilter).IsEqualTo("MyApp.Services");
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineResult()
    {
        var coverageService = new MockCoverageMapService()
            .WithMethods(CreateMethodCoverage("Test", CoverageConfidence.Medium));
        var specFinder = new MockSpecFinder(TestPaths.Project("test.spec.csx"));
        var phase = new CoverageMapPhase(coverageService, specFinder);
        var context = CreateContextWithSourceFiles();

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task ExecuteAsync_SourcePathNull_PassesNullToService()
    {
        var coverageService = new MockCoverageMapService()
            .WithMethods(CreateMethodCoverage("Test", CoverageConfidence.High));
        var specFinder = new MockSpecFinder(TestPaths.Project("test.spec.csx"));
        var phase = new CoverageMapPhase(coverageService, specFinder);
        var context = CreateContext();
        context.Set(ContextKeys.ProjectPath, TestPaths.ProjectDir);
        // Don't set SourcePath - should pass null to service
        context.Set<IReadOnlyList<string>>(ContextKeys.SourceFiles, [TestPaths.Project("Service.cs")]);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(coverageService.ComputeCoverageAsyncCalls).Count().IsEqualTo(1);
        var call = coverageService.ComputeCoverageAsyncCalls[0];
        await Assert.That(call.SourcePath).IsNull();
    }

    #endregion

    #region No Spec Files Tests

    [Test]
    public async Task ExecuteAsync_NoSpecFiles_ReturnsError()
    {
        var coverageService = new MockCoverageMapService();
        var specFinder = new MockSpecFinder(); // No spec files
        var phase = new CoverageMapPhase(coverageService, specFinder);
        var console = new MockConsole();
        var context = CreateContextWithSourceFiles(console: console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("No spec files found");
    }

    #endregion

    #region No Methods Found Tests

    [Test]
    public async Task ExecuteAsync_NoMethodsAfterFiltering_ReturnsZeroWithMessage()
    {
        var coverageService = new MockCoverageMapService(); // Returns empty result
        var specFinder = new MockSpecFinder(TestPaths.Project("test.spec.csx"));
        var phase = new CoverageMapPhase(coverageService, specFinder);
        var console = new MockConsole();
        var context = CreateContextWithSourceFiles(console: console);
        context.Set(ContextKeys.NamespaceFilter, "NonExistent.Namespace");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(99), // Should not be called
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("No methods found matching namespace filter");
    }

    #endregion

    #region Precondition Tests

    [Test]
    public async Task ExecuteAsync_ProjectPathNotSet_ReturnsError()
    {
        var coverageService = new MockCoverageMapService();
        var specFinder = new MockSpecFinder();
        var phase = new CoverageMapPhase(coverageService, specFinder);
        var console = new MockConsole();
        var context = CreateContext(console);
        context.Set<IReadOnlyList<string>>(ContextKeys.SourceFiles, [TestPaths.Project("test.cs")]);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("ProjectPath not set");
    }

    [Test]
    public async Task ExecuteAsync_SourceFilesNotSet_ReturnsError()
    {
        var coverageService = new MockCoverageMapService();
        var specFinder = new MockSpecFinder();
        var phase = new CoverageMapPhase(coverageService, specFinder);
        var console = new MockConsole();
        var context = CreateContext(console);
        context.Set(ContextKeys.ProjectPath, TestPaths.ProjectDir);
        // Don't set SourceFiles

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("SourceFiles not set");
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task ExecuteAsync_ServiceThrows_ReturnsError()
    {
        var coverageService = new MockCoverageMapService()
            .Throws(new InvalidOperationException("Coverage analysis failed"));
        var specFinder = new MockSpecFinder(TestPaths.Project("test.spec.csx"));
        var phase = new CoverageMapPhase(coverageService, specFinder);
        var console = new MockConsole();
        var context = CreateContextWithSourceFiles(console: console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("Coverage analysis failed");
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

    private static CommandContext CreateContextWithSourceFiles(MockConsole? console = null)
    {
        var context = CreateContext(console);
        context.Set(ContextKeys.ProjectPath, TestPaths.ProjectDir);
        context.Set(ContextKeys.SourcePath, TestPaths.ProjectDir);
        context.Set<IReadOnlyList<string>>(ContextKeys.SourceFiles, [TestPaths.Project("Service.cs")]);
        return context;
    }

    private static MethodCoverage CreateMethodCoverage(string methodName, CoverageConfidence confidence)
    {
        return new MethodCoverage
        {
            Method = new SourceMethod
            {
                MethodName = methodName,
                ClassName = "TestClass",
                Namespace = "TestNamespace",
                FullyQualifiedName = $"TestNamespace.TestClass.{methodName}",
                Signature = $"{methodName}()",
                SourceFile = TestPaths.Project("Test.cs"),
                LineNumber = 1
            },
            Confidence = confidence
        };
    }

    #endregion
}
