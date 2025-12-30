using DraftSpec.Cli;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;

namespace DraftSpec.Tests.Cli.Pipeline;

/// <summary>
/// Tests for CommandExecutor generic class.
/// </summary>
public class CommandExecutorTests
{
    #region ExecuteAsync Tests

    [Test]
    public async Task ExecuteAsync_CallsConfigApplier()
    {
        var mockApplier = new MockConfigApplier();
        var mockCommand = new MockCommand(0);
        var executor = new CommandExecutor<MockCommand, MockOptions>(
            mockCommand,
            _ => new MockOptions(),
            mockApplier);
        var options = new CliOptions { Path = "/test" };

        await executor.ExecuteAsync(options, CancellationToken.None);

        await Assert.That(mockApplier.ApplyCalled).IsTrue();
        await Assert.That(mockApplier.LastOptions?.Path).IsEqualTo("/test");
    }

    [Test]
    public async Task ExecuteAsync_ConvertsOptions()
    {
        var mockCommand = new MockCommand(0);
        MockOptions? convertedOptions = null;
        var executor = new CommandExecutor<MockCommand, MockOptions>(
            mockCommand,
            cli =>
            {
                convertedOptions = new MockOptions { Value = cli.Path };
                return convertedOptions;
            },
            null);
        var options = new CliOptions { Path = "/custom" };

        await executor.ExecuteAsync(options, CancellationToken.None);

        await Assert.That(convertedOptions).IsNotNull();
        await Assert.That(convertedOptions!.Value).IsEqualTo("/custom");
    }

    [Test]
    public async Task ExecuteAsync_CallsCommand()
    {
        var mockCommand = new MockCommand(0);
        var executor = new CommandExecutor<MockCommand, MockOptions>(
            mockCommand,
            _ => new MockOptions(),
            null);

        await executor.ExecuteAsync(new CliOptions(), CancellationToken.None);

        await Assert.That(mockCommand.ExecuteCalled).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_ReturnsCommandResult()
    {
        var mockCommand = new MockCommand(42);
        var executor = new CommandExecutor<MockCommand, MockOptions>(
            mockCommand,
            _ => new MockOptions(),
            null);

        var result = await executor.ExecuteAsync(new CliOptions(), CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task ExecuteAsync_NoConfigApplier_StillWorks()
    {
        var mockCommand = new MockCommand(0);
        var executor = new CommandExecutor<MockCommand, MockOptions>(
            mockCommand,
            _ => new MockOptions(),
            configApplier: null);

        var result = await executor.ExecuteAsync(new CliOptions(), CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var mockCommand = new MockCommand(0);
        var executor = new CommandExecutor<MockCommand, MockOptions>(
            mockCommand,
            _ => new MockOptions(),
            null);

        await executor.ExecuteAsync(new CliOptions(), cts.Token);

        await Assert.That(mockCommand.LastCancellationToken).IsEqualTo(cts.Token);
    }

    #endregion

    #region Helper Classes

    private class MockOptions
    {
        public string? Value { get; set; }
    }

    private class MockCommand : ICommand<MockOptions>
    {
        private readonly int _exitCode;

        public bool ExecuteCalled { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public MockCommand(int exitCode)
        {
            _exitCode = exitCode;
        }

        public Task<int> ExecuteAsync(MockOptions options, CancellationToken ct = default)
        {
            ExecuteCalled = true;
            LastCancellationToken = ct;
            return Task.FromResult(_exitCode);
        }
    }

    private class MockConfigApplier : IConfigApplier
    {
        public bool ApplyCalled { get; private set; }
        public CliOptions? LastOptions { get; private set; }

        public void ApplyConfig(CliOptions options)
        {
            ApplyCalled = true;
            LastOptions = options;
        }
    }

    #endregion
}
