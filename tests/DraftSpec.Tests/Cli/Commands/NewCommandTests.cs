using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for NewCommand option-to-context wiring.
/// Phase logic is tested in NewSpecOutputPhaseTests.
/// </summary>
public class NewCommandTests
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

    private NewCommand CreateCommand(int pipelineReturnValue = 0)
    {
        return new NewCommand(MockPipeline, _console, _fileSystem);

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
        var options = new NewOptions { Path = "/test/path", SpecName = "Test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Path).IsEqualTo("/test/path");
    }

    [Test]
    public async Task ExecuteAsync_SetsSpecNameInContext()
    {
        var command = CreateCommand();
        var options = new NewOptions { Path = "/test", SpecName = "MyFeature" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.SpecName)).IsEqualTo("MyFeature");
    }

    [Test]
    public async Task ExecuteAsync_NullSpecName_SetsNullInContext()
    {
        var command = CreateCommand();
        var options = new NewOptions { Path = "/test", SpecName = null };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.SpecName)).IsNull();
    }

    #endregion

    #region Pipeline Execution Tests

    [Test]
    public async Task ExecuteAsync_CallsPipeline()
    {
        var command = CreateCommand();
        var options = new NewOptions { Path = "/test", SpecName = "Test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext).IsNotNull();
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_Zero()
    {
        var command = CreateCommand(pipelineReturnValue: 0);
        var options = new NewOptions { Path = "/test", SpecName = "Test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_NonZero()
    {
        var command = CreateCommand(pipelineReturnValue: 1);
        var options = new NewOptions { Path = "/test", SpecName = "Test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_SetsConsoleInContext()
    {
        var command = CreateCommand();
        var options = new NewOptions { Path = "/test", SpecName = "Test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Console).IsSameReferenceAs(_console);
    }

    [Test]
    public async Task ExecuteAsync_SetsFileSystemInContext()
    {
        var command = CreateCommand();
        var options = new NewOptions { Path = "/test", SpecName = "Test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.FileSystem).IsSameReferenceAs(_fileSystem);
    }

    #endregion
}
