# ADR-002: Reporter System Design

**Status:** Accepted
**Date:** 2025-12-15
**Deciders:** DraftSpec maintainers

## Context

DraftSpec needs to emit events during spec execution to support:

- Console output showing progress (dots, spec names, errors)
- File output (JSON, HTML reports)
- Third-party integrations (CI systems, notifications)
- Custom tooling (IDE plugins, watch mode)

**Requirements:**

1. Non-blocking - reporting shouldn't slow down spec execution
2. Streaming - results available as specs complete, not just at the end
3. Multiple reporters - run console and file output simultaneously
4. Extensible - users can add custom reporters

**Alternatives considered:**

1. **Events/delegates** - Simple but hard to compose
2. **Observer pattern** - Good for streaming but complex
3. **Callback interface** - Simple, testable, composable

## Decision

Use a **callback-based IReporter interface** with async methods:

```csharp
public interface IReporter
{
    string Name { get; }

    Task OnRunStartingAsync(RunStartingContext context);
    Task OnSpecCompletedAsync(SpecResult result);
    Task OnRunCompletedAsync(SpecReport report);
}
```

**Key design choices:**

1. **Async-first** - All methods return Task to support async I/O
2. **Default implementations** - Interface provides default empty implementations so reporters only override what they need
3. **Context objects** - Each callback receives a typed context with relevant data
4. **Registration via configuration** - Reporters registered on DraftSpecConfiguration

**Usage:**

```csharp
var config = new DraftSpecConfiguration();
config.AddReporter(new ConsoleReporter());
config.AddReporter(new FileReporter("results.json"));

var runner = new SpecRunnerBuilder()
    .WithConfiguration(config)
    .Build();
```

## Consequences

### Positive

- **Streaming output** - `OnSpecCompletedAsync` enables real-time progress display
- **Multiple reporters** - Configuration supports arbitrary number of reporters
- **Testable** - Interface-based design enables easy mocking
- **Simple API** - Three clear callbacks covering run lifecycle

### Negative

- **Async overhead** - Every callback is async even for sync operations
- **Sequential execution** - Reporters called in order; slow reporter blocks others
- **No backpressure** - Fast spec execution can overwhelm slow reporters

### Neutral

- **No cancellation** - Reporters can't abort the run
- **One-way communication** - Reporters observe but don't modify results

## Implementation Notes

- `SpecRunner` calls reporters after each spec completes
- Reporter exceptions are caught and logged, not propagated
- Reporters receive immutable result objects
- `RunStartingContext` provides total spec count for progress bars
