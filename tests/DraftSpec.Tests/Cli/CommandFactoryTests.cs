using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Tests.Infrastructure;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for CommandFactory class.
/// </summary>
public class CommandFactoryTests
{
    #region Create Command Tests

    [Test]
    public async Task Create_Run_ReturnsRunCommand()
    {
        var runCommand = new MockRunCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<RunCommand>(runCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("run");

        await Assert.That(command).IsSameReferenceAs(runCommand);
    }

    [Test]
    public async Task Create_Watch_ReturnsWatchCommand()
    {
        var watchCommand = new MockWatchCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<WatchCommand>(watchCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("watch");

        await Assert.That(command).IsSameReferenceAs(watchCommand);
    }

    [Test]
    public async Task Create_List_ReturnsListCommand()
    {
        var listCommand = new MockListCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<ListCommand>(listCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("list");

        await Assert.That(command).IsSameReferenceAs(listCommand);
    }

    [Test]
    public async Task Create_Init_ReturnsInitCommand()
    {
        var initCommand = new MockInitCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<InitCommand>(initCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("init");

        await Assert.That(command).IsSameReferenceAs(initCommand);
    }

    [Test]
    public async Task Create_New_ReturnsNewCommand()
    {
        var newCommand = new MockNewCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<NewCommand>(newCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("new");

        await Assert.That(command).IsSameReferenceAs(newCommand);
    }

    [Test]
    public async Task Create_Unknown_ReturnsNull()
    {
        var serviceProvider = new MockServiceProvider();

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("unknown-command");

        await Assert.That(command).IsNull();
    }

    #endregion

    #region Case Insensitivity Tests

    [Test]
    [Arguments("RUN")]
    [Arguments("Run")]
    [Arguments("run")]
    [Arguments("rUn")]
    public async Task Create_CaseInsensitive_Works(string commandName)
    {
        var runCommand = new MockRunCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<RunCommand>(runCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create(commandName);

        await Assert.That(command).IsSameReferenceAs(runCommand);
    }

    [Test]
    [Arguments("WATCH")]
    [Arguments("Watch")]
    [Arguments("wAtCh")]
    public async Task Create_Watch_CaseInsensitive(string commandName)
    {
        var watchCommand = new MockWatchCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<WatchCommand>(watchCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create(commandName);

        await Assert.That(command).IsSameReferenceAs(watchCommand);
    }

    [Test]
    [Arguments("LIST")]
    [Arguments("List")]
    [Arguments("LiSt")]
    public async Task Create_List_CaseInsensitive(string commandName)
    {
        var listCommand = new MockListCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<ListCommand>(listCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create(commandName);

        await Assert.That(command).IsSameReferenceAs(listCommand);
    }

    [Test]
    [Arguments("INIT")]
    [Arguments("Init")]
    [Arguments("iNiT")]
    public async Task Create_Init_CaseInsensitive(string commandName)
    {
        var initCommand = new MockInitCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<InitCommand>(initCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create(commandName);

        await Assert.That(command).IsSameReferenceAs(initCommand);
    }

    [Test]
    [Arguments("NEW")]
    [Arguments("New")]
    [Arguments("nEw")]
    public async Task Create_New_CaseInsensitive(string commandName)
    {
        var newCommand = new MockNewCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<NewCommand>(newCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create(commandName);

        await Assert.That(command).IsSameReferenceAs(newCommand);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Create_EmptyString_ReturnsNull()
    {
        var serviceProvider = new MockServiceProvider();
        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("");

        await Assert.That(command).IsNull();
    }

    [Test]
    public async Task Create_Whitespace_ReturnsNull()
    {
        var serviceProvider = new MockServiceProvider();
        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("   ");

        await Assert.That(command).IsNull();
    }

    [Test]
    public async Task Create_WithExtraWhitespace_ReturnsNull()
    {
        // The factory uses ToLowerInvariant() which doesn't trim
        // so "run " becomes "run " which doesn't match "run"
        var runCommand = new MockRunCommand();
        var serviceProvider = new MockServiceProvider()
            .Register<RunCommand>(runCommand);

        var factory = new CommandFactory(serviceProvider);

        var command = factory.Create("run ");

        await Assert.That(command).IsNull();
    }

    #endregion

    #region Mock Implementations

    /// <summary>
    /// Simple mock IServiceProvider that returns pre-configured service instances.
    /// </summary>
    private class MockServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = [];

        public MockServiceProvider Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
            return this;
        }

        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var service) ? service : null;
        }
    }

    private class MockRunCommand : RunCommand
    {
        public MockRunCommand() : base(
            NullObjects.SpecFinder,
            NullObjects.RunnerFactory,
            NullObjects.Console,
            NullObjects.FormatterRegistry,
            NullObjects.ConfigLoader,
            NullObjects.FileSystem,
            NullObjects.Environment,
            NullObjects.StatsCollector,
            NullObjects.Partitioner)
        {
        }
    }

    private class MockWatchCommand : WatchCommand
    {
        public MockWatchCommand() : base(
            NullObjects.SpecFinder,
            NullObjects.RunnerFactory,
            NullObjects.FileWatcherFactory,
            NullObjects.Console,
            NullObjects.ConfigLoader,
            NullObjects.SpecChangeTracker)
        {
        }
    }

    private class MockListCommand : ListCommand
    {
        public MockListCommand() : base(
            NullObjects.Console,
            NullObjects.FileSystem,
            NullObjects.Partitioner)
        {
        }
    }

    private class MockInitCommand : InitCommand
    {
        public MockInitCommand() : base(
            NullObjects.Console,
            NullObjects.FileSystem,
            NullObjects.ProjectResolver)
        {
        }
    }

    private class MockNewCommand : NewCommand
    {
        public MockNewCommand() : base(
            NullObjects.Console,
            NullObjects.FileSystem)
        {
        }
    }

    #endregion
}
