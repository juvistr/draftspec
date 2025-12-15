namespace DraftSpec.Middleware;

/// <summary>
/// Middleware that enforces a timeout on spec execution.
/// Uses Task.Run for true timeout enforcement.
/// </summary>
public class TimeoutMiddleware : ISpecMiddleware
{
    private readonly TimeSpan _timeout;

    /// <summary>
    /// Create timeout middleware.
    /// </summary>
    /// <param name="timeout">Maximum time allowed for spec execution</param>
    public TimeoutMiddleware(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Must be positive");

        _timeout = timeout;
    }

    /// <summary>
    /// Create timeout middleware with milliseconds.
    /// </summary>
    public TimeoutMiddleware(int timeoutMs) : this(TimeSpan.FromMilliseconds(timeoutMs))
    {
    }

    public SpecResult Execute(SpecExecutionContext context, Func<SpecExecutionContext, SpecResult> next)
    {
        using var cts = new CancellationTokenSource();
        context.CancellationToken = cts.Token;

        var task = Task.Run(() => next(context));

        if (task.Wait(_timeout))
        {
            return task.Result;
        }

        // Timeout exceeded
        cts.Cancel();

        return new SpecResult(
            context.Spec,
            SpecStatus.Failed,
            context.ContextPath,
            _timeout,
            new TimeoutException($"Spec exceeded timeout of {_timeout.TotalMilliseconds}ms"));
    }
}
