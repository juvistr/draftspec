using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for DocsCommand option-to-context wiring.
/// Phase logic is tested in DocsOutputPhaseTests, SpecDiscoveryPhaseTests, etc.
/// </summary>
public class DocsCommandTests
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

    private DocsCommand CreateCommand(int pipelineReturnValue = 0)
    {
        return new DocsCommand(MockPipeline, _console, _fileSystem);

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
        var options = new DocsOptions { Path = "/test/path" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Path).IsEqualTo("/test/path");
    }

    [Test]
    public async Task ExecuteAsync_SetsFormatInContext_Markdown()
    {
        var command = CreateCommand();
        var options = new DocsOptions
        {
            Path = "/test",
            Format = DocsFormat.Markdown
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<DocsFormat>(ContextKeys.DocsFormat))
            .IsEqualTo(DocsFormat.Markdown);
    }

    [Test]
    public async Task ExecuteAsync_SetsFormatInContext_Html()
    {
        var command = CreateCommand();
        var options = new DocsOptions
        {
            Path = "/test",
            Format = DocsFormat.Html
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<DocsFormat>(ContextKeys.DocsFormat))
            .IsEqualTo(DocsFormat.Html);
    }

    [Test]
    public async Task ExecuteAsync_SetsContextFilterInContext()
    {
        var command = CreateCommand();
        var options = new DocsOptions
        {
            Path = "/test",
            Context = "UserService"
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.ContextFilter))
            .IsEqualTo("UserService");
    }

    [Test]
    public async Task ExecuteAsync_ContextFilterNull_SetsNullInContext()
    {
        var command = CreateCommand();
        var options = new DocsOptions
        {
            Path = "/test",
            Context = null
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.ContextFilter)).IsNull();
    }

    [Test]
    public async Task ExecuteAsync_SetsWithResultsInContext_True()
    {
        var command = CreateCommand();
        var options = new DocsOptions
        {
            Path = "/test",
            WithResults = true
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.WithResults)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsWithResultsInContext_False()
    {
        var command = CreateCommand();
        var options = new DocsOptions
        {
            Path = "/test",
            WithResults = false
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.WithResults)).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_SetsResultsFileInContext()
    {
        var command = CreateCommand();
        var options = new DocsOptions
        {
            Path = "/test",
            ResultsFile = "/results/output.json"
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.ResultsFile))
            .IsEqualTo("/results/output.json");
    }

    [Test]
    public async Task ExecuteAsync_ResultsFileNull_SetsNullInContext()
    {
        var command = CreateCommand();
        var options = new DocsOptions
        {
            Path = "/test",
            ResultsFile = null
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.ResultsFile)).IsNull();
    }

    [Test]
    public async Task ExecuteAsync_SetsFilterInContext()
    {
        var command = CreateCommand();
        var filter = new FilterOptions { FilterName = "add" };
        var options = new DocsOptions
        {
            Path = "/test",
            Filter = filter
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<FilterOptions>(ContextKeys.Filter))
            .IsSameReferenceAs(filter);
    }

    [Test]
    public async Task ExecuteAsync_DefaultFilter_SetsDefaultInContext()
    {
        var command = CreateCommand();
        var options = new DocsOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        var filter = _capturedContext!.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter).IsNotNull();
        await Assert.That(filter!.FilterName).IsNull();
    }

    #endregion

    #region Pipeline Execution Tests

    [Test]
    public async Task ExecuteAsync_CallsPipeline()
    {
        var command = CreateCommand();
        var options = new DocsOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext).IsNotNull();
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_Zero()
    {
        var command = CreateCommand(pipelineReturnValue: 0);
        var options = new DocsOptions { Path = "/test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_NonZero()
    {
        var command = CreateCommand(pipelineReturnValue: 1);
        var options = new DocsOptions { Path = "/test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_SetsConsoleInContext()
    {
        var command = CreateCommand();
        var options = new DocsOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Console).IsSameReferenceAs(_console);
    }

    [Test]
    public async Task ExecuteAsync_SetsFileSystemInContext()
    {
        var command = CreateCommand();
        var options = new DocsOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.FileSystem).IsSameReferenceAs(_fileSystem);
    }

    #endregion
}
