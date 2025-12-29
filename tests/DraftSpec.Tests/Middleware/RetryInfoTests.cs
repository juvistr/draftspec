using DraftSpec.Middleware;

namespace DraftSpec.Tests.Middleware;

public class RetryInfoTests
{
    [Test]
    public async Task PassedAfterRetry_FirstAttempt_ReturnsFalse()
    {
        var retryInfo = new RetryInfo { Attempts = 1, MaxRetries = 3 };

        await Assert.That(retryInfo.PassedAfterRetry).IsFalse();
    }

    [Test]
    public async Task PassedAfterRetry_SecondAttempt_ReturnsTrue()
    {
        var retryInfo = new RetryInfo { Attempts = 2, MaxRetries = 3 };

        await Assert.That(retryInfo.PassedAfterRetry).IsTrue();
    }

    [Test]
    public async Task PassedAfterRetry_MultipleAttempts_ReturnsTrue()
    {
        var retryInfo = new RetryInfo { Attempts = 5, MaxRetries = 10 };

        await Assert.That(retryInfo.PassedAfterRetry).IsTrue();
    }
}
