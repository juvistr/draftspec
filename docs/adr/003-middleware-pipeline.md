# ADR-003: Middleware Pipeline Pattern

**Status:** Accepted
**Date:** 2025-12-15
**Deciders:** DraftSpec maintainers

## Context

Cross-cutting concerns need to wrap spec execution:

- **Retry** - Re-run failed specs N times
- **Timeout** - Cancel specs exceeding a duration
- **Filtering** - Skip specs that don't match predicates
- **Profiling** - Time spec execution
- **Logging** - Record execution events

These concerns should be:

1. Composable - multiple can apply to the same spec
2. Configurable - users choose which to enable
3. Orderable - retry should wrap timeout, not vice versa
4. Extensible - users can add custom middleware

**Alternatives considered:**

1. **Attributes on specs** - Not composable, requires compile-time decisions
2. **Configuration flags** - Limited extensibility
3. **Aspect-oriented programming** - Complex, non-obvious execution order
4. **Chain-of-responsibility** - Composable, explicit ordering

## Decision

Use a **middleware pipeline pattern** similar to ASP.NET Core:

```csharp
public interface ISpecMiddleware
{
    Task<SpecResult> ExecuteAsync(
        SpecExecutionContext context,
        Func<SpecExecutionContext, Task<SpecResult>> next);
}
```

**Key design choices:**

1. **Explicit next delegate** - Middleware controls whether to continue
2. **Async throughout** - Supports timeout via `Task.WhenAny`
3. **Context object** - Carries spec info and mutable state bag
4. **Builder pattern** - Fluent API for adding middleware

**Pipeline execution:**

```
Request → Filter → Retry → Timeout → Core Execution
                                         ↓
Response ← Filter ← Retry ← Timeout ← Result
```

**Usage:**

```csharp
var runner = new SpecRunnerBuilder()
    .WithFilter(ctx => ctx.Spec.Tags.Contains("unit"))
    .WithRetry(3)
    .WithTimeout(5000)
    .Build();
```

## Consequences

### Positive

- **Composable** - Stack unlimited middleware
- **Testable** - Each middleware tested in isolation
- **Explicit order** - Registration order determines execution order
- **Short-circuit** - Middleware can skip downstream execution

### Negative

- **Order matters** - Incorrect order can cause subtle bugs (timeout outside retry)
- **Complexity** - More moving parts than simple flags
- **Learning curve** - Pattern unfamiliar to some developers

### Neutral

- **No middleware removal** - Once registered, middleware can't be removed
- **Async required** - Even sync middleware must return Task

## Implementation Notes

**SpecExecutionContext** carries:

- `Spec` - The spec being executed
- `Context` - Parent SpecContext (for hooks)
- `ContextPath` - Breadcrumb trail of descriptions
- `CancellationToken` - Set by timeout middleware
- `Items` - Dictionary for middleware to share state

**Built-in middleware:**

1. `FilterMiddleware` - Skips specs that don't match predicate
2. `RetryMiddleware` - Retries failed specs with optional delay
3. `TimeoutMiddleware` - Fails specs exceeding duration

**Pipeline building:**

- Middleware added to list in `SpecRunnerBuilder`
- `Build()` wraps core execution from last to first
- First registered is outermost in pipeline
