using DraftSpec.Cli.CoverageMap;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.CoverageMap;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.CoverageMap;

/// <summary>
/// Tests for <see cref="CoverageMapOutputPhase"/>.
/// </summary>
public class CoverageMapOutputPhaseTests
{
    #region Success Cases

    [Test]
    public async Task ExecuteAsync_MethodsCovered_ReturnsExitSuccess()
    {
        var phase = new CoverageMapOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResult(console, CreateCoveredResult());

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(999),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(CoverageMapOutputPhase.ExitSuccess);
    }

    [Test]
    public async Task ExecuteAsync_WritesOutput()
    {
        var phase = new CoverageMapOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResult(console, CreateCoveredResult());

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("Coverage Map");
    }

    #endregion

    #region GapsOnly Mode

    [Test]
    public async Task ExecuteAsync_GapsOnlyWithUncovered_ReturnsExitGapsFound()
    {
        var phase = new CoverageMapOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResult(console, CreateUncoveredResult());
        context.Set(ContextKeys.GapsOnly, true);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(CoverageMapOutputPhase.ExitGapsFound);
    }

    [Test]
    public async Task ExecuteAsync_GapsOnlyWithNoCoverage_ReturnsExitSuccess()
    {
        var phase = new CoverageMapOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResult(console, CreateCoveredResult());
        context.Set(ContextKeys.GapsOnly, true);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(CoverageMapOutputPhase.ExitSuccess);
    }

    #endregion

    #region Format Tests

    [Test]
    public async Task ExecuteAsync_JsonFormat_OutputsJson()
    {
        var phase = new CoverageMapOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResult(console, CreateCoveredResult());
        context.Set(ContextKeys.CoverageMapFormat, CoverageMapFormat.Json);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("\"summary\"");
        await Assert.That(console.Output).Contains("\"methods\"");
    }

    [Test]
    public async Task ExecuteAsync_ConsoleFormat_OutputsReadable()
    {
        var phase = new CoverageMapOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithResult(console, CreateCoveredResult());
        context.Set(ContextKeys.CoverageMapFormat, CoverageMapFormat.Console);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("Coverage Map:");
    }

    #endregion

    #region Terminal Phase Tests

    [Test]
    public async Task ExecuteAsync_TerminalPhase_DoesNotCallNextPipeline()
    {
        var phase = new CoverageMapOutputPhase();
        var context = CreateContextWithResult(new MockConsole(), CreateCoveredResult());
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(99); },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsFalse();
    }

    #endregion

    #region Error Tests

    [Test]
    public async Task ExecuteAsync_CoverageMapResultNotSet_ReturnsError()
    {
        var phase = new CoverageMapOutputPhase();
        var console = new MockConsole();
        var context = CreateContext(console);
        // Don't set CoverageMapResult

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("CoverageMapResult not set");
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

    private static CommandContext CreateContextWithResult(MockConsole console, CoverageMapResult result)
    {
        var context = CreateContext(console);
        context.Set(ContextKeys.CoverageMapResult, result);
        return context;
    }

    private static CoverageMapResult CreateCoveredResult()
    {
        return new CoverageMapResult
        {
            AllMethods =
            [
                new MethodCoverage
                {
                    Method = new SourceMethod
                    {
                        MethodName = "CreateUser",
                        ClassName = "UserService",
                        Namespace = "MyApp",
                        FullyQualifiedName = "MyApp.UserService.CreateUser",
                        Signature = "CreateUser()",
                        SourceFile = "/test/UserService.cs",
                        LineNumber = 1
                    },
                    Confidence = CoverageConfidence.High
                }
            ],
            Summary = new CoverageSummary
            {
                TotalMethods = 1,
                HighConfidence = 1
            }
        };
    }

    private static CoverageMapResult CreateUncoveredResult()
    {
        return new CoverageMapResult
        {
            AllMethods =
            [
                new MethodCoverage
                {
                    Method = new SourceMethod
                    {
                        MethodName = "UncoveredMethod",
                        ClassName = "Service",
                        Namespace = "MyApp",
                        FullyQualifiedName = "MyApp.Service.UncoveredMethod",
                        Signature = "UncoveredMethod()",
                        SourceFile = "/test/Service.cs",
                        LineNumber = 1
                    },
                    Confidence = CoverageConfidence.None
                }
            ],
            Summary = new CoverageSummary
            {
                TotalMethods = 1,
                Uncovered = 1
            }
        };
    }

    #endregion
}
