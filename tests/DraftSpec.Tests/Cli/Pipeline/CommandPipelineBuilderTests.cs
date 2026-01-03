using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline;

/// <summary>
/// Tests for <see cref="CommandPipelineBuilder"/>.
/// </summary>
public class CommandPipelineBuilderTests
{
    #region Build Tests

    [Test]
    public async Task Build_NoPhases_ReturnsZero()
    {
        var pipeline = new CommandPipelineBuilder().Build();
        var context = CreateContext();

        var result = await pipeline(context, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task Build_SinglePhase_ExecutesPhase()
    {
        var executed = false;
        var phase = new TestPhase(() => executed = true);
        var pipeline = new CommandPipelineBuilder()
            .Use(phase)
            .Build();
        var context = CreateContext();

        await pipeline(context, CancellationToken.None);

        await Assert.That(executed).IsTrue();
    }

    [Test]
    public async Task Build_MultiplePhases_ExecutesInOrder()
    {
        var order = new List<int>();
        var pipeline = new CommandPipelineBuilder()
            .Use(new TestPhase(() => order.Add(1)))
            .Use(new TestPhase(() => order.Add(2)))
            .Use(new TestPhase(() => order.Add(3)))
            .Build();
        var context = CreateContext();

        await pipeline(context, CancellationToken.None);

        await Assert.That(order).IsEquivalentTo([1, 2, 3]);
    }

    [Test]
    public async Task Build_ReturnsPhaseExitCode()
    {
        var pipeline = new CommandPipelineBuilder()
            .Use(new ExitCodePhase(42))
            .Build();
        var context = CreateContext();

        var result = await pipeline(context, CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Build_LastPhaseExitCode_Propagates()
    {
        var pipeline = new CommandPipelineBuilder()
            .Use(new TestPhase(() => { }))  // passes through
            .Use(new ExitCodePhase(99))
            .Build();
        var context = CreateContext();

        var result = await pipeline(context, CancellationToken.None);

        await Assert.That(result).IsEqualTo(99);
    }

    #endregion

    #region UseWhen Tests

    [Test]
    public async Task UseWhen_PredicateTrue_ExecutesPhase()
    {
        var executed = false;
        var pipeline = new CommandPipelineBuilder()
            .UseWhen(_ => true, new TestPhase(() => executed = true))
            .Build();
        var context = CreateContext();

        await pipeline(context, CancellationToken.None);

        await Assert.That(executed).IsTrue();
    }

    [Test]
    public async Task UseWhen_PredicateFalse_SkipsPhase()
    {
        var executed = false;
        var pipeline = new CommandPipelineBuilder()
            .UseWhen(_ => false, new TestPhase(() => executed = true))
            .Build();
        var context = CreateContext();

        await pipeline(context, CancellationToken.None);

        await Assert.That(executed).IsFalse();
    }

    [Test]
    public async Task UseWhen_PredicateFalse_ContinuesToNextPhase()
    {
        var order = new List<int>();
        var pipeline = new CommandPipelineBuilder()
            .Use(new TestPhase(() => order.Add(1)))
            .UseWhen(_ => false, new TestPhase(() => order.Add(2)))
            .Use(new TestPhase(() => order.Add(3)))
            .Build();
        var context = CreateContext();

        await pipeline(context, CancellationToken.None);

        await Assert.That(order).IsEquivalentTo([1, 3]);
    }

    [Test]
    public async Task UseWhen_PredicateReadsContext_Works()
    {
        var executed = false;
        var pipeline = new CommandPipelineBuilder()
            .UseWhen(ctx => ctx.Get<bool>("flag"), new TestPhase(() => executed = true))
            .Build();
        var context = CreateContext();
        context.Set("flag", true);

        await pipeline(context, CancellationToken.None);

        await Assert.That(executed).IsTrue();
    }

    #endregion

    #region Short-Circuit Tests

    [Test]
    public async Task Phase_DoesNotCallNext_ShortCircuits()
    {
        var secondExecuted = false;
        var pipeline = new CommandPipelineBuilder()
            .Use(new ShortCircuitPhase(1))
            .Use(new TestPhase(() => secondExecuted = true))
            .Build();
        var context = CreateContext();

        var result = await pipeline(context, CancellationToken.None);

        await Assert.That(secondExecuted).IsFalse();
        await Assert.That(result).IsEqualTo(1);
    }

    #endregion

    #region Context Sharing Tests

    [Test]
    public async Task Phase_CanModifyContext_NextPhaseSeesChanges()
    {
        string? capturedValue = null;
        var pipeline = new CommandPipelineBuilder()
            .Use(new ContextWriterPhase("key", "value"))
            .Use(new ContextReaderPhase("key", v => capturedValue = v))
            .Build();
        var context = CreateContext();

        await pipeline(context, CancellationToken.None);

        await Assert.That(capturedValue).IsEqualTo("value");
    }

    [Test]
    public async Task Phase_ContextChanges_VisibleAfterPipeline()
    {
        var pipeline = new CommandPipelineBuilder()
            .Use(new ContextWriterPhase(ContextKeys.ProjectPath, "/resolved/path"))
            .Build();
        var context = CreateContext();

        await pipeline(context, CancellationToken.None);

        await Assert.That(context.Get<string>(ContextKeys.ProjectPath)).IsEqualTo("/resolved/path");
    }

    [Test]
    public async Task Context_Get_MissingKey_ReturnsDefault()
    {
        var context = CreateContext();

        var result = context.Get<string>("nonexistent-key");

        await Assert.That(result).IsNull();
    }

    #endregion

    #region Cancellation Tests

    [Test]
    public async Task Phase_ReceivesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken? capturedToken = null;
        var phase = new TokenCapturingPhase(t => capturedToken = t);
        var pipeline = new CommandPipelineBuilder()
            .Use(phase)
            .Build();
        var context = CreateContext();

        await pipeline(context, cts.Token);

        await Assert.That(capturedToken).IsEqualTo(cts.Token);
    }

    #endregion

    #region Argument Validation Tests

    [Test]
    public void Use_NullPhase_ThrowsArgumentNullException()
    {
        var builder = new CommandPipelineBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.Use(null!));
    }

    [Test]
    public void UseWhen_NullPredicate_ThrowsArgumentNullException()
    {
        var builder = new CommandPipelineBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.UseWhen(null!, new TestPhase(() => { })));
    }

    [Test]
    public void UseWhen_NullPhase_ThrowsArgumentNullException()
    {
        var builder = new CommandPipelineBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.UseWhen(_ => true, null!));
    }

    #endregion

    #region Helper Methods

    private static CommandContext CreateContext() => new()
    {
        Path = "/test/path",
        Console = new MockConsole(),
        FileSystem = new MockFileSystem()
    };

    #endregion

    #region Helper Classes

    private class TestPhase : ICommandPhase
    {
        private readonly Action _onExecute;

        public TestPhase(Action onExecute)
        {
            _onExecute = onExecute;
        }

        public async Task<int> ExecuteAsync(
            CommandContext context,
            Func<CommandContext, CancellationToken, Task<int>> pipeline,
            CancellationToken ct)
        {
            _onExecute();
            return await pipeline(context, ct);
        }
    }

    private class ExitCodePhase : ICommandPhase
    {
        private readonly int _exitCode;

        public ExitCodePhase(int exitCode)
        {
            _exitCode = exitCode;
        }

        public Task<int> ExecuteAsync(
            CommandContext context,
            Func<CommandContext, CancellationToken, Task<int>> pipeline,
            CancellationToken ct)
        {
            return Task.FromResult(_exitCode);
        }
    }

    private class ShortCircuitPhase : ICommandPhase
    {
        private readonly int _exitCode;

        public ShortCircuitPhase(int exitCode)
        {
            _exitCode = exitCode;
        }

        public Task<int> ExecuteAsync(
            CommandContext context,
            Func<CommandContext, CancellationToken, Task<int>> pipeline,
            CancellationToken ct)
        {
            // Don't call pipeline - short circuit
            return Task.FromResult(_exitCode);
        }
    }

    private class ContextWriterPhase : ICommandPhase
    {
        private readonly string _key;
        private readonly object _value;

        public ContextWriterPhase(string key, object value)
        {
            _key = key;
            _value = value;
        }

        public async Task<int> ExecuteAsync(
            CommandContext context,
            Func<CommandContext, CancellationToken, Task<int>> pipeline,
            CancellationToken ct)
        {
            context.Items[_key] = _value;
            return await pipeline(context, ct);
        }
    }

    private class ContextReaderPhase : ICommandPhase
    {
        private readonly string _key;
        private readonly Action<string?> _onRead;

        public ContextReaderPhase(string key, Action<string?> onRead)
        {
            _key = key;
            _onRead = onRead;
        }

        public async Task<int> ExecuteAsync(
            CommandContext context,
            Func<CommandContext, CancellationToken, Task<int>> pipeline,
            CancellationToken ct)
        {
            _onRead(context.Get<string>(_key));
            return await pipeline(context, ct);
        }
    }

    private class TokenCapturingPhase : ICommandPhase
    {
        private readonly Action<CancellationToken> _onCapture;

        public TokenCapturingPhase(Action<CancellationToken> onCapture)
        {
            _onCapture = onCapture;
        }

        public async Task<int> ExecuteAsync(
            CommandContext context,
            Func<CommandContext, CancellationToken, Task<int>> pipeline,
            CancellationToken ct)
        {
            _onCapture(ct);
            return await pipeline(context, ct);
        }
    }

    #endregion
}
