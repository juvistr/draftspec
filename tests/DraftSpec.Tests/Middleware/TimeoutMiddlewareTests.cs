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
            await Task.Delay(500);
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
            await Task.Delay(500);
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
        var middleware = new TimeoutMiddleware(50);
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);
        var cancellationSignal = new ManualResetEventSlim(false);

        await middleware.ExecuteAsync(context, async ctx =>
        {
            // Wait in small increments, checking for cancellation
            for (var i = 0; i < 100; i++)
            {
                if (ctx.CancellationToken.IsCancellationRequested)
                {
                    cancellationSignal.Set();
                    break;
                }

                await Task.Delay(10);
            }

            return new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath);
        });

        // Wait for the background task to observe the cancellation
        var wasCancelled = cancellationSignal.Wait(1000);
        await Assert.That(wasCancelled).IsTrue();
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