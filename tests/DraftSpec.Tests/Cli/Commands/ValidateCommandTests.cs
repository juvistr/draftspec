using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for ValidateCommand option-to-context wiring.
/// Phase logic is tested in ValidationPhaseTests, ValidateOutputPhaseTests, etc.
/// </summary>
public class ValidateCommandTests
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

    private ValidateCommand CreateCommand(int pipelineReturnValue = 0)
    {
        return new ValidateCommand(MockPipeline, _console, _fileSystem);

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
        var options = new ValidateOptions { Path = "/test/path" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Path).IsEqualTo("/test/path");
    }

    [Test]
    public async Task ExecuteAsync_SetsExplicitFilesInContext()
    {
        var command = CreateCommand();
        var options = new ValidateOptions
        {
            Path = "/test",
            Files = ["a.spec.csx", "b.spec.csx"]
        };

        await command.ExecuteAsync(options);

        var files = _capturedContext!.Get<List<string>>(ContextKeys.ExplicitFiles);
        await Assert.That(files).IsNotNull();
        await Assert.That(files!).Contains("a.spec.csx");
        await Assert.That(files).Contains("b.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_ExplicitFilesNull_SetsNullInContext()
    {
        var command = CreateCommand();
        var options = new ValidateOptions
        {
            Path = "/test",
            Files = null
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<List<string>>(ContextKeys.ExplicitFiles)).IsNull();
    }

    [Test]
    public async Task ExecuteAsync_SetsStrictInContext_True()
    {
        var command = CreateCommand();
        var options = new ValidateOptions
        {
            Path = "/test",
            Strict = true
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.Strict)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsStrictInContext_False()
    {
        var command = CreateCommand();
        var options = new ValidateOptions
        {
            Path = "/test",
            Strict = false
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.Strict)).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_SetsQuietInContext_True()
    {
        var command = CreateCommand();
        var options = new ValidateOptions
        {
            Path = "/test",
            Quiet = true
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.Quiet)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsQuietInContext_False()
    {
        var command = CreateCommand();
        var options = new ValidateOptions
        {
            Path = "/test",
            Quiet = false
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.Quiet)).IsFalse();
    }

    #endregion

    #region Pipeline Execution Tests

    [Test]
    public async Task ExecuteAsync_CallsPipeline()
    {
        var command = CreateCommand();
        var options = new ValidateOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext).IsNotNull();
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_Zero()
    {
        var command = CreateCommand(pipelineReturnValue: 0);
        var options = new ValidateOptions { Path = "/test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_NonZero()
    {
        var command = CreateCommand(pipelineReturnValue: 2);
        var options = new ValidateOptions { Path = "/test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_SetsConsoleInContext()
    {
        var command = CreateCommand();
        var options = new ValidateOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Console).IsSameReferenceAs(_console);
    }

    [Test]
    public async Task ExecuteAsync_SetsFileSystemInContext()
    {
        var command = CreateCommand();
        var options = new ValidateOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.FileSystem).IsSameReferenceAs(_fileSystem);
    }

    #endregion
}
