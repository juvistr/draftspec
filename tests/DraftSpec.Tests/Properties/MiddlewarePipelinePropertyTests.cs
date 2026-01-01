using DraftSpec.Middleware;
using FsCheck;
using FsCheck.Fluent;

namespace DraftSpec.Tests.Properties;

/// <summary>
/// Property-based tests for middleware pipeline behavior.
/// These tests verify middleware composition properties that must hold for all inputs.
/// </summary>
public class MiddlewarePipelinePropertyTests
{
    [Test]
    public void RetryInfo_AttemptsWithinBounds()
    {
        // Property: Attempts is always in range [1, MaxRetries + 1]
        Prop.ForAll<int, int>((attempts, maxRetries) =>
        {
            var a = Math.Abs(attempts % 10) + 1; // At least 1
            var m = Math.Abs(maxRetries % 10);

            // Ensure attempts doesn't exceed maxRetries + 1
            var validAttempts = Math.Min(a, m + 1);

            var info = new RetryInfo
            {
                Attempts = validAttempts,
                MaxRetries = m
            };

            return info.Attempts >= 1 && info.Attempts <= info.MaxRetries + 1;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void RetryInfo_PassedAfterRetry_TrueOnlyWhenMultipleAttempts()
    {
        // Property: PassedAfterRetry is true iff Attempts > 1
        Prop.ForAll<int, int>((attempts, maxRetries) =>
        {
            var a = Math.Abs(attempts % 10) + 1;
            var m = Math.Abs(maxRetries % 10);

            var info = new RetryInfo
            {
                Attempts = a,
                MaxRetries = m
            };

            return info.PassedAfterRetry == (a > 1);
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void RetryMiddleware_RejectsNegativeMaxRetries()
    {
        // Property: Negative maxRetries throws ArgumentOutOfRangeException
        Prop.ForAll<int>(maxRetries =>
        {
            if (maxRetries < 0)
            {
                try
                {
                    _ = new RetryMiddleware(maxRetries);
                    return false; // Should have thrown
                }
                catch (ArgumentOutOfRangeException)
                {
                    return true; // Expected
                }
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void RetryMiddleware_AcceptsNonNegativeMaxRetries()
    {
        // Property: Non-negative maxRetries is valid
        Prop.ForAll<int>(maxRetries =>
        {
            if (maxRetries >= 0)
            {
                var normalized = maxRetries % 100; // Keep reasonable
                var middleware = new RetryMiddleware(normalized);
                return middleware != null;
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task RetryMiddleware_PassingSpecNeverRetried()
    {
        // Property: A passing spec is never retried, even with retries configured
        var callCount = 0;
        var middleware = new RetryMiddleware(3);

        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        Task<SpecResult> Pipeline(SpecExecutionContext _)
        {
            callCount++;
            return Task.FromResult(new SpecResult(spec, SpecStatus.Passed, ["ctx"]));
        }

        var result = await middleware.ExecuteAsync(context, Pipeline);

        await Assert.That(callCount).IsEqualTo(1);
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(result.RetryInfo!.Attempts).IsEqualTo(1);
    }

    [Test]
    public async Task RetryMiddleware_FailingSpecRetriedUpToMax()
    {
        // Property: A failing spec is retried up to maxRetries times
        var callCount = 0;
        var maxRetries = 3;
        var middleware = new RetryMiddleware(maxRetries);

        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        Task<SpecResult> Pipeline(SpecExecutionContext _)
        {
            callCount++;
            return Task.FromResult(new SpecResult(spec, SpecStatus.Failed, ["ctx"]));
        }

        var result = await middleware.ExecuteAsync(context, Pipeline);

        // Should be called maxRetries + 1 times (initial + retries)
        await Assert.That(callCount).IsEqualTo(maxRetries + 1);
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(result.RetryInfo!.Attempts).IsEqualTo(maxRetries + 1);
    }

    [Test]
    public async Task RetryMiddleware_EventuallyPassingSpec()
    {
        // Property: If a spec eventually passes, attempts reflect when it passed
        var callCount = 0;
        var passOnAttempt = 3;
        var middleware = new RetryMiddleware(5);

        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        Task<SpecResult> Pipeline(SpecExecutionContext _)
        {
            callCount++;
            var status = callCount >= passOnAttempt ? SpecStatus.Passed : SpecStatus.Failed;
            return Task.FromResult(new SpecResult(spec, status, ["ctx"]));
        }

        var result = await middleware.ExecuteAsync(context, Pipeline);

        await Assert.That(callCount).IsEqualTo(passOnAttempt);
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(result.RetryInfo!.Attempts).IsEqualTo(passOnAttempt);
        await Assert.That(result.RetryInfo!.PassedAfterRetry).IsTrue();
    }

    [Test]
    public async Task RetryMiddleware_ZeroRetries_NoRetryInfoAttached()
    {
        // Property: With maxRetries=0, no RetryInfo is attached
        var middleware = new RetryMiddleware(0);

        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        Task<SpecResult> Pipeline(SpecExecutionContext _) =>
            Task.FromResult(new SpecResult(spec, SpecStatus.Passed, ["ctx"]));

        var result = await middleware.ExecuteAsync(context, Pipeline);

        await Assert.That(result.RetryInfo).IsNull();
    }

    [Test]
    public async Task RetryMiddleware_SkippedSpecNotRetried()
    {
        // Property: Skipped specs are not retried
        var callCount = 0;
        var middleware = new RetryMiddleware(3);

        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        Task<SpecResult> Pipeline(SpecExecutionContext _)
        {
            callCount++;
            return Task.FromResult(new SpecResult(spec, SpecStatus.Skipped, ["ctx"]));
        }

        var result = await middleware.ExecuteAsync(context, Pipeline);

        await Assert.That(callCount).IsEqualTo(1);
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Skipped);
    }

    [Test]
    public async Task RetryMiddleware_PendingSpecNotRetried()
    {
        // Property: Pending specs are not retried
        var callCount = 0;
        var middleware = new RetryMiddleware(3);

        var spec = new SpecDefinition("pending test"); // No body = pending
        var context = CreateContext(spec);

        Task<SpecResult> Pipeline(SpecExecutionContext _)
        {
            callCount++;
            return Task.FromResult(new SpecResult(spec, SpecStatus.Pending, ["ctx"]));
        }

        var result = await middleware.ExecuteAsync(context, Pipeline);

        await Assert.That(callCount).IsEqualTo(1);
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Pending);
    }

    private static SpecExecutionContext CreateContext(SpecDefinition spec)
    {
        var context = new SpecContext("test context");
        return new SpecExecutionContext
        {
            Spec = spec,
            Context = context,
            ContextPath = ["test context"],
            HasFocused = false
        };
    }
}
