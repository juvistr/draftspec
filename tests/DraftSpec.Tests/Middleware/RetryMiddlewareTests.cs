using DraftSpec.Middleware;

namespace DraftSpec.Tests.Middleware;

public class RetryMiddlewareTests
{
    #region Constructor Validation

    [Test]
    public async Task Constructor_WithNegativeRetries_Throws()
    {
        var action = () => new RetryMiddleware(-1);

        await Assert.That(action).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Constructor_WithZeroRetries_Succeeds()
    {
        var middleware = new RetryMiddleware(0);

        await Assert.That(middleware).IsNotNull();
    }

    #endregion

    #region Retry Behavior

    [Test]
    public async Task Execute_PassingOnFirstAttempt_DoesNotRetry()
    {
        var middleware = new RetryMiddleware(3);
        var attempts = 0;
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        var result = await middleware.ExecuteAsync(context, ctx =>
        {
            attempts++;
            return Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath));
        });

        await Assert.That(attempts).IsEqualTo(1);
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task Execute_FailingThenPassing_RetriesUntilPass()
    {
        var middleware = new RetryMiddleware(3);
        var attempts = 0;
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        var result = await middleware.ExecuteAsync(context, ctx =>
        {
            attempts++;
            if (attempts < 2)
                return Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Failed, ctx.ContextPath,
                    Exception: new Exception("fail")));
            return Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath));
        });

        await Assert.That(attempts).IsEqualTo(2);
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task Execute_AlwaysFailing_StopsAfterMaxRetries()
    {
        var middleware = new RetryMiddleware(2);
        var attempts = 0;
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        var result = await middleware.ExecuteAsync(context, ctx =>
        {
            attempts++;
            return Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Failed, ctx.ContextPath,
                Exception: new Exception("fail")));
        });

        await Assert.That(attempts).IsEqualTo(3); // 1 initial + 2 retries
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Failed);
    }

    [Test]
    public async Task Execute_WithRetries_AttachesRetryInfo()
    {
        var middleware = new RetryMiddleware(3);
        var attempts = 0;
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        var result = await middleware.ExecuteAsync(context, ctx =>
        {
            attempts++;
            if (attempts < 3)
                return Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Failed, ctx.ContextPath,
                    Exception: new Exception("fail")));
            return Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath));
        });

        await Assert.That(result.RetryInfo).IsNotNull();
        await Assert.That(result.RetryInfo!.Attempts).IsEqualTo(3);
        await Assert.That(result.RetryInfo.MaxRetries).IsEqualTo(3);
    }

    [Test]
    public async Task Execute_WithZeroRetries_DoesNotAttachRetryInfo()
    {
        var middleware = new RetryMiddleware(0);
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        var result = await middleware.ExecuteAsync(context, ctx =>
            Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Passed, ctx.ContextPath)));

        await Assert.That(result.RetryInfo).IsNull();
    }

    #endregion

    #region Skipped and Pending Specs

    [Test]
    public async Task Execute_SkippedSpec_DoesNotRetry()
    {
        var middleware = new RetryMiddleware(3);
        var attempts = 0;
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        var result = await middleware.ExecuteAsync(context, ctx =>
        {
            attempts++;
            return Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Skipped, ctx.ContextPath));
        });

        await Assert.That(attempts).IsEqualTo(1);
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task Execute_PendingSpec_DoesNotRetry()
    {
        var middleware = new RetryMiddleware(3);
        var attempts = 0;
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        var result = await middleware.ExecuteAsync(context, ctx =>
        {
            attempts++;
            return Task.FromResult(new SpecResult(ctx.Spec, SpecStatus.Pending, ctx.ContextPath));
        });

        await Assert.That(attempts).IsEqualTo(1);
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Pending);
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
