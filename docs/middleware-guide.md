# Writing Custom Middleware

This guide explains how to create custom middleware for DraftSpec's spec execution pipeline.

## Overview

DraftSpec uses a middleware pipeline pattern (similar to ASP.NET Core) for cross-cutting concerns like retries, timeouts, and filtering. Middleware wraps spec execution, allowing you to:

- **Intercept** execution before and after specs run
- **Short-circuit** to skip downstream execution
- **Modify** the execution context or results
- **Share state** between middleware components

For architectural background, see [ADR-003: Middleware Pipeline Pattern](adr/003-middleware-pipeline.md).

## The ISpecMiddleware Interface

```csharp
namespace DraftSpec.Middleware;

public interface ISpecMiddleware
{
    Task<SpecResult> ExecuteAsync(
        SpecExecutionContext context,
        Func<SpecExecutionContext, Task<SpecResult>> next);
}
```

- **context**: Contains the spec definition, parent context, and mutable state bag
- **next**: Delegate to call the next middleware (or core execution)
- **Returns**: The spec result, possibly modified

## Basic Middleware Structure

```csharp
using DraftSpec.Middleware;

public class LoggingMiddleware : ISpecMiddleware
{
    public async Task<SpecResult> ExecuteAsync(
        SpecExecutionContext context,
        Func<SpecExecutionContext, Task<SpecResult>> next)
    {
        // BEFORE: runs before the spec executes
        Console.WriteLine($"Starting: {context.Spec.Description}");

        // Call next middleware or core execution
        var result = await next(context);

        // AFTER: runs after the spec completes
        Console.WriteLine($"Finished: {result.Status}");

        return result;
    }
}
```

## Execution Context

The `SpecExecutionContext` provides access to spec information and shared state:

```csharp
public class SpecExecutionContext
{
    // The spec definition being executed
    public required SpecDefinition Spec { get; init; }

    // The context containing the spec (for hooks)
    public required SpecContext Context { get; init; }

    // Path of context descriptions (e.g., ["Calculator", "add"])
    public required IReadOnlyList<string> ContextPath { get; init; }

    // Whether any specs are focused (fit)
    public required bool HasFocused { get; init; }

    // Cancellation token (set by timeout middleware)
    public CancellationToken CancellationToken { get; set; }

    // Thread-safe mutable bag for middleware state sharing
    public ConcurrentDictionary<string, object> Items { get; }
}
```

### Accessing Spec Information

```csharp
public async Task<SpecResult> ExecuteAsync(
    SpecExecutionContext context,
    Func<SpecExecutionContext, Task<SpecResult>> next)
{
    var specName = context.Spec.Description;
    var fullPath = string.Join(" ", context.ContextPath.Append(specName));
    var tags = context.Spec.Tags;

    // Check spec flags
    if (context.Spec.IsPending) { /* ... */ }
    if (context.Spec.IsSkipped) { /* ... */ }
    if (context.Spec.IsFocused) { /* ... */ }

    return await next(context);
}
```

### Sharing State Between Middleware

Use `Items` to pass data between middleware components:

```csharp
// Upstream middleware sets data
context.Items["StartTime"] = DateTime.UtcNow;

// Downstream middleware reads it
if (context.Items.TryGetValue("StartTime", out var startObj))
{
    var startTime = (DateTime)startObj;
}
```

## Execution Order and Chaining

Middleware executes in **registration order** (first registered = outermost):

```csharp
var runner = new SpecRunnerBuilder()
    .Use(new FilterMiddleware(...))    // 1st - outermost
    .Use(new RetryMiddleware(...))     // 2nd
    .Use(new TimeoutMiddleware(...))   // 3rd - innermost
    .Build();
```

Execution flow:

```
Request  → Filter → Retry → Timeout → Core Execution
                                           ↓
Response ← Filter ← Retry ← Timeout ← Result
```

**Order matters!** Common mistakes:

- **Wrong**: Timeout outside retry - each retry gets full timeout
- **Right**: Retry outside timeout - timeout applies to each attempt

## Common Patterns

### Pattern 1: Filtering/Short-circuiting

Skip specs without calling `next`:

```csharp
public class TagFilterMiddleware : ISpecMiddleware
{
    private readonly HashSet<string> _requiredTags;

    public TagFilterMiddleware(params string[] tags)
    {
        _requiredTags = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<SpecResult> ExecuteAsync(
        SpecExecutionContext context,
        Func<SpecExecutionContext, Task<SpecResult>> next)
    {
        // Short-circuit: skip if no matching tags
        if (!context.Spec.Tags.Any(t => _requiredTags.Contains(t)))
        {
            return new SpecResult(
                context.Spec,
                SpecStatus.Skipped,
                context.ContextPath);
        }

        return await next(context);
    }
}
```

### Pattern 2: Timing/Profiling

Measure execution time:

```csharp
public class TimingMiddleware : ISpecMiddleware
{
    private readonly Action<string, TimeSpan> _onComplete;

    public TimingMiddleware(Action<string, TimeSpan> onComplete)
    {
        _onComplete = onComplete;
    }

    public async Task<SpecResult> ExecuteAsync(
        SpecExecutionContext context,
        Func<SpecExecutionContext, Task<SpecResult>> next)
    {
        var sw = Stopwatch.StartNew();

        var result = await next(context);

        sw.Stop();
        _onComplete(context.Spec.Description, sw.Elapsed);

        return result;
    }
}
```

### Pattern 3: Retry with Backoff

Retry failed specs with exponential backoff:

```csharp
public class ExponentialRetryMiddleware : ISpecMiddleware
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;

    public ExponentialRetryMiddleware(int maxRetries, TimeSpan initialDelay)
    {
        _maxRetries = maxRetries;
        _initialDelay = initialDelay;
    }

    public async Task<SpecResult> ExecuteAsync(
        SpecExecutionContext context,
        Func<SpecExecutionContext, Task<SpecResult>> next)
    {
        var attempts = 0;
        SpecResult result;

        do
        {
            if (attempts > 0)
            {
                var delay = TimeSpan.FromMilliseconds(
                    _initialDelay.TotalMilliseconds * Math.Pow(2, attempts - 1));
                await Task.Delay(delay);
            }

            attempts++;
            result = await next(context);

        } while (result.Status == SpecStatus.Failed && attempts <= _maxRetries);

        return result;
    }
}
```

### Pattern 4: Timeout Enforcement

Use `Task.WhenAny` for async timeout:

```csharp
public class TimeoutMiddleware : ISpecMiddleware
{
    private readonly TimeSpan _timeout;

    public TimeoutMiddleware(TimeSpan timeout) => _timeout = timeout;

    public async Task<SpecResult> ExecuteAsync(
        SpecExecutionContext context,
        Func<SpecExecutionContext, Task<SpecResult>> next)
    {
        using var cts = new CancellationTokenSource();
        context.CancellationToken = cts.Token;

        var specTask = next(context);
        var timeoutTask = Task.Delay(_timeout, cts.Token);

        var completed = await Task.WhenAny(specTask, timeoutTask);

        if (completed == specTask)
        {
            cts.Cancel();
            return await specTask;
        }

        // Timeout exceeded
        cts.Cancel();
        return new SpecResult(
            context.Spec,
            SpecStatus.Failed,
            context.ContextPath,
            _timeout,
            new TimeoutException($"Spec exceeded {_timeout.TotalMilliseconds}ms"));
    }
}
```

### Pattern 5: Result Modification

Modify the result after execution:

```csharp
public class MetadataMiddleware : ISpecMiddleware
{
    public async Task<SpecResult> ExecuteAsync(
        SpecExecutionContext context,
        Func<SpecExecutionContext, Task<SpecResult>> next)
    {
        var result = await next(context);

        // Add custom metadata using record 'with' expression
        return result with
        {
            // Extend SpecResult with custom properties if needed
        };
    }
}
```

## Testing Middleware

Test middleware in isolation by providing mock `next` delegates:

```csharp
using DraftSpec.Middleware;

public class MyMiddlewareTests
{
    [Test]
    public async Task Execute_PassingSpec_CallsNext()
    {
        var middleware = new MyMiddleware();
        var nextCalled = false;
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        var result = await middleware.ExecuteAsync(context, ctx =>
        {
            nextCalled = true;
            return Task.FromResult(new SpecResult(
                ctx.Spec, SpecStatus.Passed, ctx.ContextPath));
        });

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Passed);
    }

    [Test]
    public async Task Execute_FilteredSpec_DoesNotCallNext()
    {
        var middleware = new MyFilterMiddleware(shouldRun: false);
        var nextCalled = false;
        var spec = new SpecDefinition("test", () => { });
        var context = CreateContext(spec);

        var result = await middleware.ExecuteAsync(context, ctx =>
        {
            nextCalled = true;
            return Task.FromResult(new SpecResult(
                ctx.Spec, SpecStatus.Passed, ctx.ContextPath));
        });

        await Assert.That(nextCalled).IsFalse();
        await Assert.That(result.Status).IsEqualTo(SpecStatus.Skipped);
    }

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
```

## Registering Middleware

### Direct Registration

```csharp
var runner = new SpecRunnerBuilder()
    .Use(new MyMiddleware())
    .Build();
```

### Via Built-in Helpers

```csharp
var runner = new SpecRunnerBuilder()
    .WithFilter(ctx => ctx.Spec.Tags.Contains("unit"))
    .WithRetry(3, delayMs: 100)
    .WithTimeout(5000)
    .Build();
```

### Via Plugins

Create a middleware plugin for reusable middleware:

```csharp
using DraftSpec.Plugins;

public class MetricsMiddlewarePlugin : IMiddlewarePlugin
{
    public string Name => "Metrics";
    public string Version => "1.0.0";

    private readonly IMetricsCollector _collector;

    public MetricsMiddlewarePlugin(IMetricsCollector collector)
    {
        _collector = collector;
    }

    public void Initialize(IPluginContext context)
    {
        // Setup code here
    }

    public void RegisterMiddleware(SpecRunnerBuilder builder)
    {
        builder.Use(new MetricsMiddleware(_collector));
    }

    public void Dispose()
    {
        // Cleanup code here
    }
}
```

Register the plugin:

```csharp
var config = new DraftSpecConfiguration()
    .UsePlugin(new MetricsMiddlewarePlugin(collector));

var runner = new SpecRunnerBuilder()
    .WithConfiguration(config)
    .Build();
```

See [ADR-004: Plugin Architecture](adr/004-plugin-architecture.md) for more on plugins.

## Best Practices

1. **Keep middleware focused** - Each middleware should do one thing well
2. **Always call `next`** unless intentionally short-circuiting
3. **Handle exceptions** - Wrap `next()` in try/catch if you need to handle failures
4. **Consider async** - Even simple middleware must return `Task<SpecResult>`
5. **Use `Items` sparingly** - Prefer explicit parameters over hidden state
6. **Order carefully** - Test middleware combinations to ensure correct behavior
7. **Thread safety** - Use `Items` (ConcurrentDictionary) for parallel execution

## Built-in Middleware Reference

| Middleware | Purpose | Builder Method |
|------------|---------|----------------|
| `FilterMiddleware` | Skip specs not matching predicate | `WithFilter()`, `WithTagFilter()`, `WithNameFilter()` |
| `RetryMiddleware` | Retry failed specs | `WithRetry(maxRetries, delayMs)` |
| `TimeoutMiddleware` | Fail specs exceeding duration | `WithTimeout(ms)` |
| `CoverageMiddleware` | Track per-spec code coverage | `WithCoverage()` |
