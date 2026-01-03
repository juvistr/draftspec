using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for CoverageMapCommand option-to-context wiring.
/// Phase logic is tested in CoverageMapPhaseTests, SourceDiscoveryPhaseTests, etc.
/// Service logic is tested in CoverageMapServiceTests.
/// </summary>
public class CoverageMapCommandTests
{
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;
    private CommandContext? _capturedContext;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
        _capturedContext = null;
    }

    private CoverageMapCommand CreateCommand(int pipelineReturnValue = 0)
    {
        return new CoverageMapCommand(MockPipeline, _console, _fileSystem);

        Task<int> MockPipeline(CommandContext context, CancellationToken ct)
        {
            _capturedContext = context;
            return Task.FromResult(pipelineReturnValue);
        }
    }

    #region Context Wiring Tests

    [Test]
    public async Task ExecuteAsync_SetsPathInContext()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = "/test/source" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Path).IsEqualTo("/test/source");
    }

    [Test]
    public async Task ExecuteAsync_SetsSourcePathInContext()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = "/test/source" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.SourcePath))
            .IsEqualTo("/test/source");
    }

    [Test]
    public async Task ExecuteAsync_SetsSpecPathInContext()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = "/test",
            SpecPath = "/test/specs"
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.SpecPath))
            .IsEqualTo("/test/specs");
    }

    [Test]
    public async Task ExecuteAsync_SpecPathNull_SetsNullInContext()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = "/test",
            SpecPath = null
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.SpecPath)).IsNull();
    }

    [Test]
    public async Task ExecuteAsync_SetsFormatInContext_Console()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = "/test",
            Format = CoverageMapFormat.Console
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<CoverageMapFormat>(ContextKeys.CoverageMapFormat))
            .IsEqualTo(CoverageMapFormat.Console);
    }

    [Test]
    public async Task ExecuteAsync_SetsFormatInContext_Json()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = "/test",
            Format = CoverageMapFormat.Json
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<CoverageMapFormat>(ContextKeys.CoverageMapFormat))
            .IsEqualTo(CoverageMapFormat.Json);
    }

    [Test]
    public async Task ExecuteAsync_SetsGapsOnlyInContext_True()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = "/test",
            GapsOnly = true
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.GapsOnly)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsGapsOnlyInContext_False()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = "/test",
            GapsOnly = false
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.GapsOnly)).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_SetsNamespaceFilterInContext()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = "/test",
            NamespaceFilter = "MyApp.Services"
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.NamespaceFilter))
            .IsEqualTo("MyApp.Services");
    }

    [Test]
    public async Task ExecuteAsync_NamespaceFilterNull_SetsNullInContext()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = "/test",
            NamespaceFilter = null
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.NamespaceFilter)).IsNull();
    }

    #endregion

    #region Pipeline Execution Tests

    [Test]
    public async Task ExecuteAsync_CallsPipeline()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext).IsNotNull();
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_Zero()
    {
        var command = CreateCommand(pipelineReturnValue: 0);
        var options = new CoverageMapOptions { SourcePath = "/test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_NonZero()
    {
        var command = CreateCommand(pipelineReturnValue: 42);
        var options = new CoverageMapOptions { SourcePath = "/test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task ExecuteAsync_SetsConsoleInContext()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Console).IsSameReferenceAs(_console);
    }

    [Test]
    public async Task ExecuteAsync_SetsFileSystemInContext()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.FileSystem).IsSameReferenceAs(_fileSystem);
    }

    #endregion
}
