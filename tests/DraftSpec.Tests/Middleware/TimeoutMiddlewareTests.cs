using DraftSpec.Middleware;

namespace DraftSpec.Tests.Middleware;

public class TimeoutMiddlewareTests
{
    #region Constructor Validation

    [Test]
    public async Task Constructor_WithZeroTimeout_Throws()
    {
        var action = () => new TimeoutMiddleware(TimeSpan.Zero);

        await Assert.That(action).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Constructor_WithNegativeTimeout_Throws()
    {
        var action = () => new TimeoutMiddleware(TimeSpan.FromMilliseconds(-1));

        await Assert.That(action).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Constructor_WithZeroMs_Throws()
    {
        var action = () => new TimeoutMiddleware(0);

        await Assert.That(action).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Constructor_WithNegativeMs_Throws()
    {
        var action = () => new TimeoutMiddleware(-100);

        await Assert.That(action).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Constructor_WithPositiveTimeout_Succeeds()
    {
        var middleware = new TimeoutMiddleware(TimeSpan.FromSeconds(1));

        await Assert.That(middleware).IsNotNull();
    }

    #endregion

    #region Timeout Behavior

    [Test]
    public async Task Execute_FastSpec_Passes()
    {
        var middleware = new TimeoutMiddleware(5000);
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        var result = await middleware.ExecuteAsync(context, ctx =>
            Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath)));

        await Assert.That(result.Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task Execute_SlowSpec_FailsWithTimeoutException()
    {
        var middleware = new TimeoutMiddleware(50);
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        var result = await middleware.ExecuteAsync(context, async ctx =>
        {
            // Use a much longer delay than timeout to ensure reliable timeout triggering
            await Task.Delay(5000);
            return new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath);
        });

        await Assert.That(result.Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(result.Exception).IsNotNull();
        await Assert.That(result.Exception).IsTypeOf<TimeoutException>();
    }

    [Test]
    public async Task Execute_SlowSpec_ReportsTimeoutDuration()
    {
        var middleware = new TimeoutMiddleware(50);
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        var result = await middleware.ExecuteAsync(context, async ctx =>
        {
            // Use a much longer delay than timeout to ensure reliable timeout triggering
            await Task.Delay(5000);
            return new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath);
        });

        await Assert.That(result.Duration.TotalMilliseconds).IsGreaterThanOrEqualTo(50);
    }

    #endregion

    #region Cancellation Token

    [Test]
    public async Task Execute_SetsCancellationToken()
    {
        var middleware = new TimeoutMiddleware(5000);
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);
        CancellationToken capturedToken = default;

        await middleware.ExecuteAsync(context, ctx =>
        {
            capturedToken = ctx.CancellationToken;
            return Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath));
        });

        await Assert.That(capturedToken).IsNotEqualTo(default);
    }

    [Test]
    public async Task Execute_OnTimeout_CancelsCancellationToken()
    {
        // Use longer timeout for CI reliability (timing-sensitive test)
        var middleware = new TimeoutMiddleware(200);
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);
        var taskStarted = new TaskCompletionSource();
        CancellationToken capturedToken = default;

        await middleware.ExecuteAsync(context, async ctx =>
        {
            capturedToken = ctx.CancellationToken;
            taskStarted.SetResult();

            // Wait longer than timeout, checking for cancellation
            try
            {
                await Task.Delay(5000, ctx.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected - token was cancelled
            }

            return new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath);
        });

        // Ensure the task started before middleware returned
        await taskStarted.Task;

        // The token should be cancelled after timeout
        await Assert.That(capturedToken.IsCancellationRequested).IsTrue();
    }

    #endregion

    #region Result Preservation

    [Test]
    public async Task Execute_FailingSpec_PreservesOriginalException()
    {
        var middleware = new TimeoutMiddleware(5000);
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);
        var originalException = new InvalidOperationException("original");

        var result = await middleware.ExecuteAsync(context, ctx =>
            Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Failed, ctx.ContextPath,
                Exception: originalException)));

        await Assert.That(result.Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(result.Exception).IsSameReferenceAs(originalException);
    }

    #endregion

    private static SpecExecutionContext CreateContext(SpecDefinition spec)
    {
        var specContext = new SpecContext("test");
        return new SpecExecutionContext
        {
            Spec = spec,
            Context = specContext,
            ContextPath = ["test"],
            HasFocused = false
        };
    }
}
