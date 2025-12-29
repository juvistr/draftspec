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
        var results = await runner.RunAsync(context);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(attempts).IsEqualTo(2);
    }

    [Test]
    [NotInParallel]
    public async Task SpecRunnerBuilder_WithTimeout_ConfiguresTimeoutMiddleware()
    {
        var context = new SpecContext("test");
        // Use a large gap (10s delay vs 100ms timeout) to avoid CI flakiness
        context.AddSpec(new SpecDefinition("slow", async () => await Task.Delay(10_000)));

        var runner = new SpecRunnerBuilder().WithTimeout(100).Build();
        var results = await runner.RunAsync(context);

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
        await runner.RunAsync(context);

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
        await runner.RunAsync(context);

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
            if (attempts == 1) await Task.Delay(5000); // First attempt times out (5s >> 50ms)
            // Second attempt succeeds quickly
        }));

        // Retry wraps Timeout - each attempt gets its own 50ms timeout
        // Use a large gap (1000ms delay vs 50ms timeout) to avoid flakiness
        var runner = new SpecRunnerBuilder()
            .WithRetry(2)
            .WithTimeout(50)
            .Build();
        var results = await runner.RunAsync(context);

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
        await runner.RunAsync(context);

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
        await runner.RunAsync(context);

        await Assert.That(order[0]).IsEqualTo("middleware-start");
        await Assert.That(order[1]).IsEqualTo("before");
        await Assert.That(order[2]).IsEqualTo("spec");
        await Assert.That(order[3]).IsEqualTo("after");
        await Assert.That(order[4]).IsEqualTo("middleware-end");
    }

    #endregion

    #region Exception Handling

    [Test]
    public async Task Pipeline_MiddlewareThrowsBeforeNext_PropagatesException()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("test", () => { }));

        var runner = new SpecRunnerBuilder()
            .Use(new ThrowingMiddleware(new InvalidOperationException("middleware failed"), throwBeforeNext: true))
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => runner.Run(context));
        await Assert.That(exception.Message).IsEqualTo("middleware failed");
    }

    [Test]
    public async Task Pipeline_MiddlewareThrowsAfterNext_PropagatesException()
    {
        var context = new SpecContext("test");
        var specExecuted = false;
        context.AddSpec(new SpecDefinition("test", () => specExecuted = true));

        var runner = new SpecRunnerBuilder()
            .Use(new ThrowingMiddleware(new InvalidOperationException("cleanup failed"), throwBeforeNext: false))
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => runner.Run(context));
        await Assert.That(exception.Message).IsEqualTo("cleanup failed");
        await Assert.That(specExecuted).IsTrue(); // Spec should have run before exception
    }

    [Test]
    public async Task Pipeline_InnerMiddlewareThrows_OuterMiddlewareCleanupRuns()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("test", () => { }));
        var outerCleanupRan = false;

        var runner = new SpecRunnerBuilder()
            .Use(new WrapperMiddlewareWithCleanup(
                () => { },
                () => outerCleanupRan = true))
            .Use(new ThrowingMiddleware(new InvalidOperationException("inner failed"), throwBeforeNext: true))
            .Build();

        Assert.Throws<InvalidOperationException>(() => runner.Run(context));
        await Assert.That(outerCleanupRan).IsTrue();
    }

    [Test]
    public async Task Pipeline_SpecThrows_MiddlewareCleanupRuns()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("failing spec", () => throw new Exception("spec failed")));
        var cleanupRan = false;

        var runner = new SpecRunnerBuilder()
            .Use(new WrapperMiddlewareWithCleanup(
                () => { },
                () => cleanupRan = true))
            .Build();

        var results = await runner.RunAsync(context);

        await Assert.That(cleanupRan).IsTrue();
        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
    }

    [Test]
    public async Task Pipeline_MiddlewareThrows_LaterMiddlewareNotExecuted()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("test", () => { }));
        var secondMiddlewareRan = false;

        var runner = new SpecRunnerBuilder()
            .Use(new ThrowingMiddleware(new InvalidOperationException("first failed"), throwBeforeNext: true))
            .Use(new TestMiddleware(() => secondMiddlewareRan = true))
            .Build();

        Assert.Throws<InvalidOperationException>(() => runner.Run(context));
        await Assert.That(secondMiddlewareRan).IsFalse();
    }

    [Test]
    public async Task Pipeline_MultipleMiddleware_ExceptionPropagatesThroughAll()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("test", () => { }));
        var cleanupOrder = new List<string>();

        var runner = new SpecRunnerBuilder()
            .Use(new WrapperMiddlewareWithCleanup(
                () => { },
                () => cleanupOrder.Add("outer")))
            .Use(new WrapperMiddlewareWithCleanup(
                () => { },
                () => cleanupOrder.Add("middle")))
            .Use(new ThrowingMiddleware(new InvalidOperationException("inner failed"), throwBeforeNext: true))
            .Build();

        Assert.Throws<InvalidOperationException>(() => runner.Run(context));

        // Cleanup should run in reverse order (inner to outer)
        await Assert.That(cleanupOrder).Count().IsEqualTo(2);
        await Assert.That(cleanupOrder[0]).IsEqualTo("middle");
        await Assert.That(cleanupOrder[1]).IsEqualTo("outer");
    }

    [Test]
    public async Task Pipeline_AsyncMiddlewareThrows_PropagatesException()
    {
        var context = new SpecContext("test");
        context.AddSpec(new SpecDefinition("test", () => { }));

        var runner = new SpecRunnerBuilder()
            .Use(new AsyncThrowingMiddleware())
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => runner.Run(context));
        await Assert.That(exception.Message).IsEqualTo("async middleware failed");
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

    private class ThrowingMiddleware : ISpecMiddleware
    {
        private readonly Exception _exception;
        private readonly bool _throwBeforeNext;

        public ThrowingMiddleware(Exception exception, bool throwBeforeNext)
        {
            _exception = exception;
            _throwBeforeNext = throwBeforeNext;
        }

        public async Task<SpecResult> ExecuteAsync(SpecExecutionContext context,
            Func<SpecExecutionContext, Task<SpecResult>> next)
        {
            if (_throwBeforeNext)
                throw _exception;

            var result = await next(context);
            throw _exception;
        }
    }

    private class WrapperMiddlewareWithCleanup : ISpecMiddleware
    {
        private readonly Action _before;
        private readonly Action _cleanup;

        public WrapperMiddlewareWithCleanup(Action before, Action cleanup)
        {
            _before = before;
            _cleanup = cleanup;
        }

        public async Task<SpecResult> ExecuteAsync(SpecExecutionContext context,
            Func<SpecExecutionContext, Task<SpecResult>> next)
        {
            _before();
            try
            {
                return await next(context);
            }
            finally
            {
                _cleanup();
            }
        }
    }

    private class AsyncThrowingMiddleware : ISpecMiddleware
    {
        public async Task<SpecResult> ExecuteAsync(SpecExecutionContext context,
            Func<SpecExecutionContext, Task<SpecResult>> next)
        {
            await Task.Yield(); // Force async continuation
            throw new InvalidOperationException("async middleware failed");
        }
    }

    #endregion
}