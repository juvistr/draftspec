# Architecture Review

## Summary

DraftSpec employs a well-designed **Composite + Builder + Visitor** pattern. The architecture is clean and focused, but has limited extension points for reporters, formatters, and execution customization.

**Assessment:** STRONG FOUNDATION with CLEAR EVOLUTION PATH

## Current Architecture

### Pattern: Composite + Builder + Visitor

```
Tree Construction (Builder)     Execution (Visitor)
        │                              │
        ▼                              ▼
┌─────────────┐               ┌─────────────┐
│   Dsl.cs    │───defines───▶│ SpecContext │
│ describe()  │               │   (tree)    │
│    it()     │               └──────┬──────┘
└─────────────┘                      │
                                     ▼
                              ┌─────────────┐
                              │ SpecRunner  │
                              │  (visitor)  │
                              └──────┬──────┘
                                     │
                                     ▼
                              ┌─────────────┐
                              │ SpecResult  │
                              │  (output)   │
                              └─────────────┘
```

### Domain Model (Excellent)

```
SpecContext (Composite Node)
├── Children: List<SpecContext>
├── Specs: List<SpecDefinition>
└── Hooks: before/after actions

SpecDefinition (Leaf)
├── Description: string
├── Body: Action?
└── Flags: IsPending, IsSkipped, IsFocused

SpecResult (Value Object)
├── Status: enum
├── Duration: TimeSpan
├── ContextPath: IReadOnlyList<string>
└── Exception: Exception?
```

## SOLID Assessment

### Single Responsibility Principle

| Component | Status | Notes |
|-----------|--------|-------|
| SpecDefinition | PASS | Single concern: spec metadata |
| SpecContext | PASS | Single concern: tree node |
| SpecResult | PASS | Single concern: execution result |
| SpecRunner | PASS | Single concern: tree traversal |
| Expectations | PASS | Each handles one type domain |
| **Dsl.cs** | **FAIL** | 6+ concerns mixed together |

### Open/Closed Principle

**Open for extension:**
- New expectation types via expect() overloads
- New assertion methods via extension methods

**Closed (violations):**
- Cannot add custom reporters without modifying Dsl.cs
- Cannot customize execution pipeline
- Cannot add custom formatters
- Hard-coded exit code behavior

### Interface Segregation

**Concern:** Almost no interfaces defined. Limits extensibility.

**Needed interfaces:**
- `ISpecReporter`
- `IResultFormatter`
- `ISpecRunner`
- `IExpectation<T>`

### Dependency Inversion

**Good:** AsyncLocal provides threading abstraction

**Poor:**
- Dsl.run() creates SpecRunner directly
- Console output hard-coded
- No dependency injection

## Architectural Issues

### 1. Reporting Layer (Currently Embedded in Dsl.cs)

**Problem:** Output formatting violates OCP - cannot add reporters without modification.

**Current location:** `Dsl.cs:264-425` (150+ lines)

**Recommended extraction:**

```csharp
public interface ISpecReporter
{
    void Report(SpecContext rootContext, List<SpecResult> results);
}

public class ConsoleReporter : ISpecReporter
{
    private readonly IResultFormatter _formatter;

    public void Report(SpecContext rootContext, List<SpecResult> results)
    {
        var output = _formatter.Format(results);
        Console.WriteLine(output);
    }
}

// Enable extensibility
public static void run(ISpecReporter? reporter = null)
{
    reporter ??= new ConsoleReporter(new DefaultFormatter());
    // ...
}
```

**Benefits:**
- OCP compliance
- Custom reporters (JUnit XML, TAP, TeamCity)
- Testable without Console mocking

### 2. Execution Pipeline (No Extension Points)

**Problem:** Cannot add:
- Custom lifecycle hooks
- Middleware (retries, timeouts)
- Parallel execution

**Recommended pattern:**

```csharp
public interface ISpecExecutionPipeline
{
    SpecResult Execute(SpecDefinition spec, SpecContext context);
}

public interface ISpecMiddleware
{
    SpecResult Execute(SpecDefinition spec, SpecContext context,
                       Func<SpecResult> next);
}

// Usage
config.AddMiddleware<RetryMiddleware>(maxRetries: 3);
config.AddMiddleware<TimeoutMiddleware>(timeout: 30.Seconds());
```

### 3. No Extension Points for Custom Matchers

**Problem:** Cannot add domain-specific assertions:
```csharp
// Not possible today
expect(email).toBeValidEmail();
expect(date).toBeBusinessDay();
```

**Recommendation:** Document extension via extension methods:
```csharp
public static class EmailExpectations
{
    public static void toBeValidEmail(this StringExpectation exp)
    {
        // Custom matcher implementation
    }
}
```

### 4. No Spec Filtering API

**Problem:** Cannot run specs by pattern, tag, or filter.

**Recommendation:**
```csharp
public class SpecFilter
{
    public string? NamePattern { get; set; }
    public List<string> Tags { get; set; } = [];
    public Func<SpecDefinition, bool>? CustomFilter { get; set; }
}

run(filter: new SpecFilter { NamePattern = ".*validation.*" });
```

## Recommended Architecture Evolution

### Phase 1: Separation of Concerns

```
src/DraftSpec/
├── Core/
│   ├── SpecContext.cs
│   ├── SpecDefinition.cs
│   ├── SpecResult.cs
│   └── SpecStatus.cs
├── DSL/
│   ├── Dsl.cs              # Reduced to context management
│   └── Spec.cs
├── Expectations/           # Already good
├── Execution/
│   ├── ISpecRunner.cs      # New interface
│   ├── SpecRunner.cs
│   ├── IExecutionPipeline.cs
│   └── DefaultPipeline.cs
├── Reporting/
│   ├── IReporter.cs
│   ├── ConsoleReporter.cs
│   └── JsonReporter.cs
└── Formatting/
    ├── IFormatter.cs
    ├── ResultFormatter.cs
    └── DurationFormatter.cs
```

### Phase 2: Extensibility Layer

```csharp
public class DraftSpecConfiguration
{
    public IList<IReporter> Reporters { get; }
    public IList<ISpecMiddleware> Middleware { get; }
    public bool ParallelExecution { get; set; }
    public int MaxParallelism { get; set; }
}

DraftSpecConfig.Configure(config =>
{
    config.AddReporter<JUnitReporter>();
    config.AddMiddleware<RetryMiddleware>();
    config.SetParallelExecution(maxDegreeOfParallelism: 4);
});
```

### Phase 3: Advanced Features

- Parallel execution engine
- Watch mode
- Coverage reporting hooks
- Custom assertion matchers
- Snapshot testing

## Design Patterns Applied

### Well-Applied

| Pattern | Location | Usage |
|---------|----------|-------|
| Composite | SpecContext | Tree structure |
| Builder | Dsl | Fluent API |
| Visitor | SpecRunner | Tree traversal |
| Strategy (implicit) | Expectations | Type dispatch |
| Template Method | SpecRunner | Hook ordering |
| Null Object | Hooks | Optional actions |

### Missing (Opportunities)

| Pattern | Benefit |
|---------|---------|
| Factory | Reporter creation |
| Chain of Responsibility | Execution middleware |
| Observer | Execution events |
| Adapter | Output formats |
| Decorator | Spec wrapping (retries) |

## Comparison to Industry Standards

### vs. Jest (JavaScript)
- Similar DSL (describe/it/expect)
- Similar focus/skip (fit/xit)
- Missing: Custom matchers, snapshot testing, watch mode
- Better: Type safety, compile-time checks

### vs. RSpec (Ruby)
- Similar nested context structure
- Similar hook system
- Missing: Subject/let DSL, shared examples
- Better: Performance, type safety

## Key Recommendations

### Must Do
1. Extract reporting layer with IReporter interface
2. Add ISpecRunner interface
3. Separate formatting from output

### Should Do
4. Add execution pipeline with middleware
5. Plugin registration system
6. Spec filtering API

### Could Do
7. Parallel execution engine
8. Advanced matcher API
9. Snapshot testing

## ADRs Needed

Consider documenting these decisions:

- **ADR-002:** Reporter Abstraction Pattern
- **ADR-003:** Execution Pipeline Design
- **ADR-004:** Plugin System Design
- **ADR-005:** Breaking Changes Policy
