using DraftSpec.Mcp;
using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Mcp.Services;

public class ExecutionRateLimiterTests
{
    #region CurrentConcurrentExecutions

    [Test]
    public async Task CurrentConcurrentExecutions_Initially_ReturnsZero()
    {
        var options = new McpOptions { MaxConcurrentExecutions = 3 };
        using var limiter = new ExecutionRateLimiter(options);

        var result = limiter.CurrentConcurrentExecutions;

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task CurrentConcurrentExecutions_AfterAcquire_ReturnsOne()
    {
        var options = new McpOptions { MaxConcurrentExecutions = 3 };
        using var limiter = new ExecutionRateLimiter(options);

        await limiter.TryAcquireAsync();

        var result = limiter.CurrentConcurrentExecutions;

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task CurrentConcurrentExecutions_AfterMultipleAcquires_ReturnsCorrectCount()
    {
        var options = new McpOptions { MaxConcurrentExecutions = 5 };
        using var limiter = new ExecutionRateLimiter(options);

        await limiter.TryAcquireAsync();
        await limiter.TryAcquireAsync();
        await limiter.TryAcquireAsync();

        var result = limiter.CurrentConcurrentExecutions;

        await Assert.That(result).IsEqualTo(3);
    }

    [Test]
    public async Task CurrentConcurrentExecutions_AfterRelease_Decrements()
    {
        var options = new McpOptions { MaxConcurrentExecutions = 3 };
        using var limiter = new ExecutionRateLimiter(options);

        await limiter.TryAcquireAsync();
        await limiter.TryAcquireAsync();
        limiter.Release();

        var result = limiter.CurrentConcurrentExecutions;

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task CurrentConcurrentExecutions_AtMaxCapacity_ReturnsMax()
    {
        var options = new McpOptions { MaxConcurrentExecutions = 2 };
        using var limiter = new ExecutionRateLimiter(options);

        await limiter.TryAcquireAsync();
        await limiter.TryAcquireAsync();

        var result = limiter.CurrentConcurrentExecutions;

        await Assert.That(result).IsEqualTo(2);
    }

    #endregion

    #region TryAcquireAsync

    [Test]
    public async Task TryAcquireAsync_UnderLimit_ReturnsTrue()
    {
        var options = new McpOptions { MaxConcurrentExecutions = 2 };
        using var limiter = new ExecutionRateLimiter(options);

        var result = await limiter.TryAcquireAsync();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task TryAcquireAsync_AtLimit_ReturnsFalse()
    {
        var options = new McpOptions { MaxConcurrentExecutions = 1 };
        using var limiter = new ExecutionRateLimiter(options);

        await limiter.TryAcquireAsync();
        var result = await limiter.TryAcquireAsync();

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryAcquireAsync_AfterRelease_CanAcquireAgain()
    {
        var options = new McpOptions { MaxConcurrentExecutions = 1 };
        using var limiter = new ExecutionRateLimiter(options);

        await limiter.TryAcquireAsync();
        limiter.Release();
        var result = await limiter.TryAcquireAsync();

        await Assert.That(result).IsTrue();
    }

    #endregion

    #region PerMinuteLimit

    [Test]
    public async Task TryAcquireAsync_ExceedsPerMinuteLimit_ReturnsFalse()
    {
        var options = new McpOptions
        {
            MaxConcurrentExecutions = 10,
            MaxExecutionsPerMinute = 3
        };
        using var limiter = new ExecutionRateLimiter(options);

        // Acquire up to the per-minute limit
        await limiter.TryAcquireAsync();
        limiter.Release();
        await limiter.TryAcquireAsync();
        limiter.Release();
        await limiter.TryAcquireAsync();
        limiter.Release();

        // Next acquire should fail due to per-minute limit
        var result = await limiter.TryAcquireAsync();

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryAcquireAsync_PerMinuteLimitExceeded_ReleasesSemaphore()
    {
        var options = new McpOptions
        {
            MaxConcurrentExecutions = 10,
            MaxExecutionsPerMinute = 1
        };
        using var limiter = new ExecutionRateLimiter(options);

        // Exhaust per-minute limit
        await limiter.TryAcquireAsync();
        limiter.Release();

        // Try again - should fail but not hold semaphore
        var result = await limiter.TryAcquireAsync();

        await Assert.That(result).IsFalse();
        // Semaphore should still be at full capacity (10)
        await Assert.That(limiter.CurrentConcurrentExecutions).IsEqualTo(0);
    }

    #endregion

    #region Cancellation

    [Test]
    public async Task TryAcquireAsync_CancelledToken_ThrowsOperationCancelledException()
    {
        var options = new McpOptions { MaxConcurrentExecutions = 2 };
        using var limiter = new ExecutionRateLimiter(options);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await limiter.TryAcquireAsync(cts.Token));
    }

    #endregion

    #region Dispose

    [Test]
    public async Task Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var options = new McpOptions { MaxConcurrentExecutions = 2 };
        var limiter = new ExecutionRateLimiter(options);

        limiter.Dispose();
        limiter.Dispose(); // Should not throw

        await Assert.That(true).IsTrue(); // Test passes if no exception
    }

    #endregion

    #region Thread Safety

    [Test]
    public async Task TryAcquireAsync_ConcurrentRequests_RespectsPerMinuteLimit()
    {
        // Arrange: Allow only 10 per minute, but launch 50 concurrent requests
        const int perMinuteLimit = 10;
        const int concurrentRequests = 50;
        var options = new McpOptions
        {
            MaxConcurrentExecutions = concurrentRequests, // High concurrency limit
            MaxExecutionsPerMinute = perMinuteLimit
        };
        using var limiter = new ExecutionRateLimiter(options);

        // Act: Launch all requests concurrently
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => limiter.TryAcquireAsync())
            .ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert: Exactly perMinuteLimit should succeed
        var successCount = results.Count(r => r);
        await Assert.That(successCount).IsEqualTo(perMinuteLimit);
    }

    [Test]
    public async Task TryAcquireAsync_ConcurrentRequests_RespectsConcurrencyLimit()
    {
        // Arrange: Allow only 5 concurrent, but launch 20 concurrent requests
        const int concurrencyLimit = 5;
        const int concurrentRequests = 20;
        var options = new McpOptions
        {
            MaxConcurrentExecutions = concurrencyLimit,
            MaxExecutionsPerMinute = 1000 // High per-minute limit
        };
        using var limiter = new ExecutionRateLimiter(options);

        // Act: Launch all requests concurrently
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => limiter.TryAcquireAsync())
            .ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert: Exactly concurrencyLimit should succeed
        var successCount = results.Count(r => r);
        await Assert.That(successCount).IsEqualTo(concurrencyLimit);
    }

    [Test]
    public async Task TryAcquireAsync_HighConcurrency_NoRaceConditions()
    {
        // Arrange: Test with multiple iterations to catch race conditions
        const int iterations = 100;
        const int perMinuteLimit = 5;
        const int threadsPerIteration = 20;

        for (var i = 0; i < iterations; i++)
        {
            var options = new McpOptions
            {
                MaxConcurrentExecutions = threadsPerIteration,
                MaxExecutionsPerMinute = perMinuteLimit
            };
            using var limiter = new ExecutionRateLimiter(options);

            // Act: Launch requests from multiple threads simultaneously
            var tasks = Enumerable.Range(0, threadsPerIteration)
                .Select(_ => Task.Run(() => limiter.TryAcquireAsync()))
                .ToArray();
            var results = await Task.WhenAll(tasks);

            // Assert: Never exceed limit (race condition would cause > limit)
            var successCount = results.Count(r => r);
            await Assert.That(successCount).IsLessThanOrEqualTo(perMinuteLimit);
        }
    }

    #endregion
}
