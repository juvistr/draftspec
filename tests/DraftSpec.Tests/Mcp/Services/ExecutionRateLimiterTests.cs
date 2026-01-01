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
}
