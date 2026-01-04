using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for InitCommand option-to-context wiring.
/// Phase logic is tested in InitOutputPhaseTests and ProjectDiscoveryPhaseTests.
/// </summary>
public class InitCommandTests
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

    private InitCommand CreateCommand(int pipelineReturnValue = 0)
    {
        return new InitCommand(MockPipeline, _console, _fileSystem);

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
        var options = new InitOptions { Path = "/test/path" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Path).IsEqualTo("/test/path");
    }

    [Test]
    public async Task ExecuteAsync_SetsForceInContext_True()
    {
        var command = CreateCommand();
        var options = new InitOptions
        {
            Path = "/test",
            Force = true
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.Force)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsForceInContext_False()
    {
        var command = CreateCommand();
        var options = new InitOptions
        {
            Path = "/test",
            Force = false
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.Force)).IsFalse();
    }

    #endregion

    #region Pipeline Execution Tests

    [Test]
    public async Task ExecuteAsync_CallsPipeline()
    {
        var command = CreateCommand();
        var options = new InitOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext).IsNotNull();
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_Zero()
    {
        var command = CreateCommand(pipelineReturnValue: 0);
        var options = new InitOptions { Path = "/test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_NonZero()
    {
        var command = CreateCommand(pipelineReturnValue: 1);
        var options = new InitOptions { Path = "/test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_SetsConsoleInContext()
    {
        var command = CreateCommand();
        var options = new InitOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Console).IsSameReferenceAs(_console);
    }

    [Test]
    public async Task ExecuteAsync_SetsFileSystemInContext()
    {
        var command = CreateCommand();
        var options = new InitOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.FileSystem).IsSameReferenceAs(_fileSystem);
    }

    #endregion
}
