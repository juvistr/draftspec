using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for ListCommand option-to-context wiring.
/// Phase logic is tested in ListOutputPhaseTests, SpecDiscoveryPhaseTests, etc.
/// </summary>
public class ListCommandTests
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

    private ListCommand CreateCommand(int pipelineReturnValue = 0)
    {
        return new ListCommand(MockPipeline, _console, _fileSystem);

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
        var options = new ListOptions { Path = "/test/path" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Path).IsEqualTo("/test/path");
    }

    [Test]
    public async Task ExecuteAsync_SetsListFormatInContext_Tree()
    {
        var command = CreateCommand();
        var options = new ListOptions
        {
            Path = "/test",
            Format = ListFormat.Tree
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<ListFormat>(ContextKeys.ListFormat))
            .IsEqualTo(ListFormat.Tree);
    }

    [Test]
    public async Task ExecuteAsync_SetsListFormatInContext_Flat()
    {
        var command = CreateCommand();
        var options = new ListOptions
        {
            Path = "/test",
            Format = ListFormat.Flat
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<ListFormat>(ContextKeys.ListFormat))
            .IsEqualTo(ListFormat.Flat);
    }

    [Test]
    public async Task ExecuteAsync_SetsListFormatInContext_Json()
    {
        var command = CreateCommand();
        var options = new ListOptions
        {
            Path = "/test",
            Format = ListFormat.Json
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<ListFormat>(ContextKeys.ListFormat))
            .IsEqualTo(ListFormat.Json);
    }

    [Test]
    public async Task ExecuteAsync_SetsShowLineNumbersInContext_True()
    {
        var command = CreateCommand();
        var options = new ListOptions
        {
            Path = "/test",
            ShowLineNumbers = true
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.ShowLineNumbers)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsShowLineNumbersInContext_False()
    {
        var command = CreateCommand();
        var options = new ListOptions
        {
            Path = "/test",
            ShowLineNumbers = false
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.ShowLineNumbers)).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_SetsFocusedOnlyInContext_True()
    {
        var command = CreateCommand();
        var options = new ListOptions
        {
            Path = "/test",
            FocusedOnly = true
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.FocusedOnly)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsFocusedOnlyInContext_False()
    {
        var command = CreateCommand();
        var options = new ListOptions
        {
            Path = "/test",
            FocusedOnly = false
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.FocusedOnly)).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_SetsPendingOnlyInContext_True()
    {
        var command = CreateCommand();
        var options = new ListOptions
        {
            Path = "/test",
            PendingOnly = true
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.PendingOnly)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsPendingOnlyInContext_False()
    {
        var command = CreateCommand();
        var options = new ListOptions
        {
            Path = "/test",
            PendingOnly = false
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.PendingOnly)).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_SetsSkippedOnlyInContext_True()
    {
        var command = CreateCommand();
        var options = new ListOptions
        {
            Path = "/test",
            SkippedOnly = true
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.SkippedOnly)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsSkippedOnlyInContext_False()
    {
        var command = CreateCommand();
        var options = new ListOptions
        {
            Path = "/test",
            SkippedOnly = false
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.SkippedOnly)).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_SetsFilterInContext()
    {
        var command = CreateCommand();
        var filter = new FilterOptions { FilterName = "add" };
        var options = new ListOptions
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
        var options = new ListOptions { Path = "/test" };

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
        var options = new ListOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext).IsNotNull();
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_Zero()
    {
        var command = CreateCommand(pipelineReturnValue: 0);
        var options = new ListOptions { Path = "/test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineReturnValue_NonZero()
    {
        var command = CreateCommand(pipelineReturnValue: 1);
        var options = new ListOptions { Path = "/test" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_SetsConsoleInContext()
    {
        var command = CreateCommand();
        var options = new ListOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Console).IsSameReferenceAs(_console);
    }

    [Test]
    public async Task ExecuteAsync_SetsFileSystemInContext()
    {
        var command = CreateCommand();
        var options = new ListOptions { Path = "/test" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.FileSystem).IsSameReferenceAs(_fileSystem);
    }

    #endregion
}
