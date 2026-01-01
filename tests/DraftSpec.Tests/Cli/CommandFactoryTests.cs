using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for CommandFactory class.
/// The factory returns executor functions that wrap command invocation.
/// </summary>
public class CommandFactoryTests
{
    #region Create Command Tests

    [Test]
    public async Task Create_Run_ReturnsExecutor()
    {
        var factory = CreateFactory();

        var executor = factory.Create("run");

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    public async Task Create_Watch_ReturnsExecutor()
    {
        var factory = CreateFactory();

        var executor = factory.Create("watch");

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    public async Task Create_List_ReturnsExecutor()
    {
        var factory = CreateFactory();

        var executor = factory.Create("list");

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    public async Task Create_Validate_ReturnsExecutor()
    {
        var factory = CreateFactory();

        var executor = factory.Create("validate");

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    public async Task Create_Init_ReturnsExecutor()
    {
        var factory = CreateFactory();

        var executor = factory.Create("init");

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    public async Task Create_New_ReturnsExecutor()
    {
        var factory = CreateFactory();

        var executor = factory.Create("new");

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    public async Task Create_Schema_ReturnsExecutor()
    {
        var factory = CreateFactory();

        var executor = factory.Create("schema");

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    public async Task Create_Flaky_ReturnsExecutor()
    {
        var factory = CreateFactory();

        var executor = factory.Create("flaky");

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    public async Task Create_Estimate_ReturnsExecutor()
    {
        var factory = CreateFactory();

        var executor = factory.Create("estimate");

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    public async Task Create_Unknown_ReturnsNull()
    {
        var factory = CreateFactory();

        var executor = factory.Create("unknown-command");

        await Assert.That(executor).IsNull();
    }

    #endregion

    #region Case Insensitivity Tests

    [Test]
    [Arguments("RUN")]
    [Arguments("Run")]
    [Arguments("run")]
    [Arguments("rUn")]
    public async Task Create_Run_CaseInsensitive(string commandName)
    {
        var factory = CreateFactory();

        var executor = factory.Create(commandName);

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    [Arguments("WATCH")]
    [Arguments("Watch")]
    [Arguments("wAtCh")]
    public async Task Create_Watch_CaseInsensitive(string commandName)
    {
        var factory = CreateFactory();

        var executor = factory.Create(commandName);

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    [Arguments("LIST")]
    [Arguments("List")]
    [Arguments("LiSt")]
    public async Task Create_List_CaseInsensitive(string commandName)
    {
        var factory = CreateFactory();

        var executor = factory.Create(commandName);

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    [Arguments("VALIDATE")]
    [Arguments("Validate")]
    [Arguments("vAlIdAtE")]
    public async Task Create_Validate_CaseInsensitive(string commandName)
    {
        var factory = CreateFactory();

        var executor = factory.Create(commandName);

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    [Arguments("INIT")]
    [Arguments("Init")]
    [Arguments("iNiT")]
    public async Task Create_Init_CaseInsensitive(string commandName)
    {
        var factory = CreateFactory();

        var executor = factory.Create(commandName);

        await Assert.That(executor).IsNotNull();
    }

    [Test]
    [Arguments("NEW")]
    [Arguments("New")]
    [Arguments("nEw")]
    public async Task Create_New_CaseInsensitive(string commandName)
    {
        var factory = CreateFactory();

        var executor = factory.Create(commandName);

        await Assert.That(executor).IsNotNull();
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Create_EmptyString_ReturnsNull()
    {
        var factory = CreateFactory();

        var executor = factory.Create("");

        await Assert.That(executor).IsNull();
    }

    [Test]
    public async Task Create_Whitespace_ReturnsNull()
    {
        var factory = CreateFactory();

        var executor = factory.Create("   ");

        await Assert.That(executor).IsNull();
    }

    [Test]
    public async Task Create_WithExtraWhitespace_ReturnsNull()
    {
        // The factory uses ToLowerInvariant() which doesn't trim
        // so "run " becomes "run " which doesn't match "run"
        var factory = CreateFactory();

        var executor = factory.Create("run ");

        await Assert.That(executor).IsNull();
    }

    #endregion

    #region Helpers

    private static CommandFactory CreateFactory()
    {
        return new CommandFactory(
            NullObjects.ConfigApplier,
            CreateRunCommand,
            CreateWatchCommand,
            CreateListCommand,
            CreateValidateCommand,
            CreateInitCommand,
            CreateNewCommand,
            CreateSchemaCommand,
            CreateFlakyCommand,
            CreateEstimateCommand);
    }

    private static RunCommand CreateRunCommand() => new(
        NullObjects.SpecFinder,
        NullObjects.RunnerFactory,
        NullObjects.Console,
        NullObjects.FormatterRegistry,
        NullObjects.FileSystem,
        NullObjects.Environment,
        NullObjects.StatsCollector,
        NullObjects.Partitioner,
        NullObjects.GitService,
        NullObjects.HistoryService);

    private static WatchCommand CreateWatchCommand() => new(
        NullObjects.SpecFinder,
        NullObjects.RunnerFactory,
        NullObjects.FileWatcherFactory,
        NullObjects.Console,
        NullObjects.SpecChangeTracker);

    private static ListCommand CreateListCommand() => new(
        NullObjects.Console,
        NullObjects.FileSystem);

    private static ValidateCommand CreateValidateCommand() => new(
        NullObjects.Console,
        NullObjects.FileSystem);

    private static InitCommand CreateInitCommand() => new(
        NullObjects.Console,
        NullObjects.FileSystem,
        NullObjects.ProjectResolver);

    private static NewCommand CreateNewCommand() => new(
        NullObjects.Console,
        NullObjects.FileSystem);

    private static SchemaCommand CreateSchemaCommand() => new(
        NullObjects.Console,
        NullObjects.FileSystem);

    private static FlakyCommand CreateFlakyCommand() => new(
        NullObjects.HistoryService,
        NullObjects.Console,
        NullObjects.FileSystem);

    private static EstimateCommand CreateEstimateCommand() => new(
        NullObjects.RuntimeEstimator,
        NullObjects.HistoryService,
        NullObjects.Console,
        NullObjects.FileSystem);

    #endregion
}
