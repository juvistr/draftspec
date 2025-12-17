using DraftSpec.Middleware;

namespace DraftSpec.Tests.Middleware;

public class PipelineTests
{
    #region Builder

    [Test]
    public async Task SpecRunnerBuilder_Build_ReturnsRunner()
    {
        var builder = new SpecRunnerBuilder();
        var runner = builder.Build();

        await Assert.That(runner).IsNotNull();
    }

    [Test]
    public async Task SpecRunnerBuilder_WithRetry_ConfiguresRetryMiddleware()
    {
        var context = new SpecContext("test");
        var attempts = 0;
        context.AddSpec(new SpecDefinition("flaky", () =>
        {
            attempts++;
            if (attempts < 2) throw new Exception("fail");
        }));

        var runner = new SpecRunnerBuilder().WithRetry(2).Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(attempts).IsEqualTo(2);
    }

    [Test]
    public async Task SpecRunnerBuilder_WithTimeout_ConfiguresTimeoutMiddleware()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("slow", async () => await Task.Delay(500)));

        var runner = new SpecRunnerBuilder().WithTimeout(50).Build();
        var results = runner.Run(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(results[0].Exception).IsTypeOf<TimeoutException>();
    }

    [Test]
    public async Task SpecRunnerBuilder_Use_AddsCustomMiddleware()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("test", () => { }));
        var middlewareCalled = false;

        var runner = new SpecRunnerBuilder()
            .Use(new TestMiddleware(() => middlewareCalled = true))
            .Build();
        runner.Run(context);

        await Assert.That(middlewareCalled).IsTrue();
    }

    #endregion

    #region Middleware Order

    [Test]
    public async Task Pipeline_ExecutesMiddlewareInOrder()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("test", () => { }));
        var order = new List<string>();

        var runner = new SpecRunnerBuilder()
            .Use(new TestMiddleware(() => order.Add("first")))
            .Use(new TestMiddleware(() => order.Add("second")))
            .Use(new TestMiddleware(() => order.Add("third")))
            .Build();
        runner.Run(context);

        await Assert.That(order[0]).IsEqualTo("first");
        await Assert.That(order[1]).IsEqualTo("second");
        await Assert.That(order[2]).IsEqualTo("third");
    }

    [Test]
    public async Task Pipeline_RetryWithIndividualTimeouts()
    {
        // When retry wraps timeout, each retry attempt has its own timeout
        // Order matters: WithRetry first, then WithTimeout
        var context = new SpecContext("test");
        var attempts = 0;
        context.AddSpec(new SpecDefinition("test", async () =>
        {
            attempts++;
            if (attempts == 1) await Task.Delay(1000); // First attempt times out (1s >> 50ms)
            // Second attempt succeeds quickly
        }));

        // Retry wraps Timeout - each attempt gets its own 50ms timeout
        // Use a large gap (1000ms delay vs 50ms timeout) to avoid flakiness
        var runner = new SpecRunnerBuilder()
            .WithRetry(2)
            .WithTimeout(50)
            .Build();
        var results = runner.Run(context);

        // Should pass on second attempt after first timed out
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(attempts).IsEqualTo(2);
    }

    #endregion

    #region Middleware State Sharing

    [Test]
    public async Task Pipeline_MiddlewareCanShareState()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("test", () => { }));
        string? capturedValue = null;

        var runner = new SpecRunnerBuilder()
            .Use(new StateWriterMiddleware("shared-key", "test-value"))
            .Use(new StateReaderMiddleware("shared-key", v => capturedValue = v))
            .Build();
        runner.Run(context);

        await Assert.That(capturedValue).IsEqualTo("test-value");
    }

    #endregion

    #region Integration with Hooks

    [Test]
    public async Task Pipeline_HooksRunInsideMiddleware()
    {
        var context = new SpecContext("test");
        var order = new List<string>();
        context.BeforeEach = () =>
        {
            order.Add("before");
            return Task.CompletedTask;
        };
        context.AfterEach = () =>
        {
            order.Add("after");
            return Task.CompletedTask;
        };
        context.AddSpec(new SpecDefinition("test", () => order.Add("spec")));

        var runner = new SpecRunnerBuilder()
            .Use(new WrapperMiddleware(() => order.Add("middleware-start"), () => order.Add("middleware-end")))
            .Build();
        runner.Run(context);

        await Assert.That(order[0]).IsEqualTo("middleware-start");
        await Assert.That(order[1]).IsEqualTo("before");
        await Assert.That(order[2]).IsEqualTo("spec");
        await Assert.That(order[3]).IsEqualTo("after");
        await Assert.That(order[4]).IsEqualTo("middleware-end");
    }

    #endregion

    #region Test Helpers

    private class TestMiddleware : ISpecMiddleware
    {
        private readonly Action _onExecute;

        public TestMiddleware(Action onExecute)
        {
            _onExecute = onExecute;
        }

        public async Task<SpecResult> ExecuteAsync(SpecExecutionContext context,
            Func<SpecExecutionContext, Task<SpecResult>> next)
        {
            _onExecute();
            return await next(context);
        }
    }

    private class StateWriterMiddleware : ISpecMiddleware
    {
        private readonly string _key;
        private readonly object _value;

        public StateWriterMiddleware(string key, object value)
        {
            _key = key;
            _value = value;
        }

        public async Task<SpecResult> ExecuteAsync(SpecExecutionContext context,
            Func<SpecExecutionContext, Task<SpecResult>> next)
        {
            context.Items[_key] = _value;
            return await next(context);
        }
    }

    private class StateReaderMiddleware : ISpecMiddleware
    {
        private readonly string _key;
        private readonly Action<string?> _capture;

        public StateReaderMiddleware(string key, Action<string?> capture)
        {
            _key = key;
            _capture = capture;
        }

        public async Task<SpecResult> ExecuteAsync(SpecExecutionContext context,
            Func<SpecExecutionContext, Task<SpecResult>> next)
        {
            if (context.Items.TryGetValue(_key, out var value))
                _capture(value as string);
            return await next(context);
        }
    }

    private class WrapperMiddleware : ISpecMiddleware
    {
        private readonly Action _before;
        private readonly Action _after;

        public WrapperMiddleware(Action before, Action after)
        {
            _before = before;
            _after = after;
        }

        public async Task<SpecResult> ExecuteAsync(SpecExecutionContext context,
            Func<SpecExecutionContext, Task<SpecResult>> next)
        {
            _before();
            var result = await next(context);
            _after();
            return result;
        }
    }

    #endregion
}