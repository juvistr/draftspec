namespace DraftSpec.Middleware;

/// <summary>
/// Middleware that retries failed specs.
/// </summary>
public class RetryMiddleware : ISpecMiddleware
{
    private readonly int _maxRetries;
    private readonly TimeSpan _delay;

    /// <summary>
    /// Create retry middleware.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retries (0 = no retries)</param>
    /// <param name="delay">Delay between retries (default: no delay)</param>
    public RetryMiddleware(int maxRetries, TimeSpan delay = default)
    {
        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Must be non-negative");

        _maxRetries = maxRetries;
        _delay = delay;
    }

    public async Task<SpecResult> ExecuteAsync(SpecExecutionContext context, Func<SpecExecutionContext, Task<SpecResult>> next)
    {
        var attempts = 0;
        SpecResult result;

        do
        {
            attempts++;
            result = await next(context);

            if (result.Status != SpecStatus.Failed || attempts > _maxRetries)
                break;

            if (_delay > TimeSpan.Zero)
                await Task.Delay(_delay);

        } while (true);

        // Attach retry info if we had retries configured
        if (_maxRetries > 0)
        {
            return result with
            {
                RetryInfo = new RetryInfo
                {
                    Attempts = attempts,
                    MaxRetries = _maxRetries
                }
            };
        }

        return result;
    }
}
