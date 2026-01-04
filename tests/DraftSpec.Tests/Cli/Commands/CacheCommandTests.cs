using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for CacheCommand option-to-context wiring.
/// Phase logic is tested in CacheOperationPhaseTests.
/// </summary>
public class CacheCommandTests
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

    private CacheCommand CreateCommand(int pipelineReturnValue = 0)
    {
        return new CacheCommand(MockPipeline, _console, _fileSystem);

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
        var options = new CacheOptions { Path = "/some/path", Subcommand = "stats" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Path).IsEqualTo("/some/path");
    }

    [Test]
    public async Task ExecuteAsync_SetsSubcommandInContext_Stats()
    {
        var command = CreateCommand();
        var options = new CacheOptions { Path = ".", Subcommand = "stats" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.CacheSubcommand)).IsEqualTo("stats");
    }

    [Test]
    public async Task ExecuteAsync_SetsSubcommandInContext_Clear()
    {
        var command = CreateCommand();
        var options = new CacheOptions { Path = ".", Subcommand = "clear" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.CacheSubcommand)).IsEqualTo("clear");
    }

    [Test]
    public async Task ExecuteAsync_DefaultSubcommand_SetsStatsInContext()
    {
        var command = CreateCommand();
        var options = new CacheOptions { Path = "." }; // Default Subcommand is "stats"

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.CacheSubcommand)).IsEqualTo("stats");
    }

    #endregion

    #region Pipeline Execution Tests

    [Test]
    public async Task ExecuteAsync_CallsPipeline()
    {
        var command = CreateCommand();
        var options = new CacheOptions { Path = ".", Subcommand = "stats" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext).IsNotNull();
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_Zero()
    {
        var command = CreateCommand(pipelineReturnValue: 0);
        var options = new CacheOptions { Path = ".", Subcommand = "stats" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_NonZero()
    {
        var command = CreateCommand(pipelineReturnValue: 1);
        var options = new CacheOptions { Path = ".", Subcommand = "stats" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_SetsConsoleInContext()
    {
        var command = CreateCommand();
        var options = new CacheOptions { Path = ".", Subcommand = "stats" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Console).IsSameReferenceAs(_console);
    }

    [Test]
    public async Task ExecuteAsync_SetsFileSystemInContext()
    {
        var command = CreateCommand();
        var options = new CacheOptions { Path = ".", Subcommand = "stats" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.FileSystem).IsSameReferenceAs(_fileSystem);
    }

    #endregion
}
