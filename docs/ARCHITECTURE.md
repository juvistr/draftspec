# DraftSpec Architecture

This document describes the high-level architecture of DraftSpec, an RSpec-inspired testing framework for .NET.

## Component Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         User Code                                │
│  (CSX scripts using Dsl.* or classes extending Spec)            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      DSL Layer                                   │
│  Dsl.cs (static API)          Spec.cs (class-based)             │
│  describe/it/expect           Describe/It/Expect                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Core Domain                                   │
│  SpecContext ─────► SpecDefinition                              │
│  (tree structure)   (single test)                               │
│        │                                                         │
│        └──► Hooks (BeforeAll/AfterAll/BeforeEach/AfterEach)     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Execution Layer                                │
│  SpecRunner ──► SpecRunnerBuilder                               │
│       │              │                                           │
│       │              └──► Middleware Pipeline                    │
│       │                   (Retry, Timeout, Filter)              │
│       │                                                          │
│       └──► SpecExecutionContext (shared state)                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Plugin System                                  │
│  IPlugin ──► IReporterPlugin                                    │
│          ├─► IFormatterPlugin                                   │
│          └─► IMiddlewarePlugin                                  │
│                                                                  │
│  Registries: FormatterRegistry, Reporter callbacks              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Output Layer                                   │
│  IFormatter ──► JsonFormatter                                   │
│             ├─► MarkdownFormatter                               │
│             └─► HtmlFormatter                                   │
│                                                                  │
│  IReporter (callbacks during execution)                         │
└─────────────────────────────────────────────────────────────────┘
```

## Layer Details

### DSL Layer

The DSL layer provides two ways to write specs:

**Static API (Dsl.cs)** - Recommended for CSX scripts:
```csharp
using static DraftSpec.Dsl;

describe("Calculator", () =>
{
    it("adds numbers", () => expect(1 + 1).toBe(2));
});
```

**Class-based API (Spec.cs)** - Alternative for traditional test classes:
```csharp
public class CalculatorSpecs : Spec
{
    public override void Define()
    {
        Describe("Calculator", () =>
        {
            It("adds numbers", () => Expect(1 + 1).ToBe(2));
        });
    }
}
```

### Core Domain

**SpecContext** represents a `describe` or `context` block:
- Contains nested `SpecContext` children (tree structure)
- Contains `SpecDefinition` specs
- Contains hooks: `BeforeAll`, `AfterAll`, `BeforeEach`, `AfterEach`
- Maintains focus state for `fdescribe`/`fit`

**SpecDefinition** represents a single `it` block:
- Description text
- Body action (nullable = pending)
- Tags for filtering
- Focus/skip flags

**SpecResult** captures execution outcome:
- Status: Passed, Failed, Pending, Skipped
- Duration timing
- Exception details (if failed)
- Context path (breadcrumb trail)

### Execution Layer

**SpecRunner** walks the spec tree and executes specs:
1. Scans tree for focused specs
2. Runs BeforeAll hooks (root → leaf)
3. For each spec:
   - Runs BeforeEach hooks (root → leaf)
   - Executes middleware pipeline → spec body
   - Runs AfterEach hooks (leaf → root)
4. Runs AfterAll hooks (leaf → root)
5. Collects and returns results

**SpecRunnerBuilder** provides fluent configuration:
```csharp
var runner = new SpecRunnerBuilder()
    .WithRetry(3)
    .WithTimeout(5000)
    .WithFilter(ctx => ctx.Spec.Tags.Contains("unit"))
    .Build();
```

**SpecExecutionContext** carries execution state through middleware:
- Current spec and context
- Shared Items dictionary
- CancellationToken for timeout

### Middleware Pipeline

Middleware wraps spec execution with cross-cutting concerns. Each middleware receives a context and `next` delegate:

```csharp
public class RetryMiddleware : ISpecMiddleware
{
    public async Task ExecuteAsync(SpecExecutionContext context, Func<Task> next)
    {
        for (int i = 0; i < _maxRetries; i++)
        {
            try
            {
                await next();
                return;
            }
            catch when (i < _maxRetries - 1) { }
        }
    }
}
```

**Built-in Middleware:**
- `FilterMiddleware` - Skip specs that don't match predicate
- `RetryMiddleware` - Retry failed specs N times
- `TimeoutMiddleware` - Cancel specs exceeding duration

**Execution Order:** Filter → Retry → Timeout → Spec Body

### Plugin System

Plugins extend DraftSpec without modifying core code:

**IReporterPlugin** - Receive execution events:
```csharp
public class ConsoleReporter : IReporter
{
    public Task OnSpecCompletedAsync(SpecResult result) { ... }
    public Task OnRunCompletedAsync(SpecReport report) { ... }
}
```

**IFormatterPlugin** - Output report in custom format:
```csharp
public class XmlFormatter : IFormatter
{
    public string Format(SpecReport report) { ... }
}
```

**IMiddlewarePlugin** - Add custom middleware:
```csharp
public class LoggingPlugin : IMiddlewarePlugin
{
    public ISpecMiddleware GetMiddleware(IPluginContext ctx) => new LoggingMiddleware();
}
```

### Output Layer

**IFormatter** transforms `SpecReport` to text:
- `JsonFormatter` - Machine-readable JSON
- `MarkdownFormatter` - Human-readable markdown
- `HtmlFormatter` - Styled HTML report

**IReporter** receives events during execution:
- `OnRunStartingAsync` - Before first spec
- `OnSpecCompletedAsync` - After each spec
- `OnRunCompletedAsync` - After all specs

## Extension Points

### 1. Custom Middleware

Implement `ISpecMiddleware` to wrap spec execution:

```csharp
public class ProfilingMiddleware : ISpecMiddleware
{
    public async Task ExecuteAsync(SpecExecutionContext context, Func<Task> next)
    {
        var sw = Stopwatch.StartNew();
        await next();
        Console.WriteLine($"{context.Spec.Description}: {sw.ElapsedMilliseconds}ms");
    }
}
```

Register via builder:
```csharp
new SpecRunnerBuilder().WithMiddleware(new ProfilingMiddleware())
```

### 2. Custom Formatters

Implement `IFormatter` and register with `FormatterRegistry`:

```csharp
FormatterRegistry.Register("xml", new XmlFormatter());
var output = FormatterRegistry.GetByName("xml").Format(report);
```

### 3. Custom Reporters

Implement `IReporter` and add to configuration:

```csharp
var config = new DraftSpecConfiguration();
config.AddReporter(new SlackReporter());
```

### 4. Custom Matchers (Extension Methods)

All expectation classes expose `Actual` and `Expression` properties, enabling custom matchers via extension methods:

**DateTime Matcher Example:**
```csharp
public static class DateExpectationExtensions
{
    public static void toBeAfter(this Expectation<DateTime> exp, DateTime other)
    {
        if (exp.Actual <= other)
            throw new AssertionException(
                $"Expected {exp.Expression} to be after {other:O}, but was {exp.Actual:O}");
    }

    public static void toBeBefore(this Expectation<DateTime> exp, DateTime other)
    {
        if (exp.Actual >= other)
            throw new AssertionException(
                $"Expected {exp.Expression} to be before {other:O}, but was {exp.Actual:O}");
    }
}

// Usage:
expect(order.ShippedDate).toBeAfter(order.OrderDate);
```

**String Matcher Example:**
```csharp
public static class StringMatcherExtensions
{
    public static void toBeValidEmail(this StringExpectation exp)
    {
        if (exp.Actual is null || !exp.Actual.Contains('@') || !exp.Actual.Contains('.'))
            throw new AssertionException(
                $"Expected {exp.Expression} to be a valid email, but was \"{exp.Actual}\"");
    }
}

// Usage:
expect(user.Email).toBeValidEmail();
```

**Collection Matcher Example:**
```csharp
public static class CollectionMatcherExtensions
{
    public static void toAllSatisfy<T>(
        this CollectionExpectation<T> exp,
        Func<T, bool> predicate,
        string description)
    {
        var failing = exp.Actual.Where(x => !predicate(x)).ToList();
        if (failing.Count > 0)
            throw new AssertionException(
                $"Expected all items in {exp.Expression} to {description}, " +
                $"but {failing.Count} item(s) failed");
    }
}

// Usage:
expect(numbers).toAllSatisfy(n => n > 0, "be positive");
```

**Available Properties by Expectation Type:**

| Class | Exposed Properties |
|-------|-------------------|
| `Expectation<T>` | `Actual`, `Expression` |
| `StringExpectation` | `Actual`, `Expression` |
| `BoolExpectation` | `Actual`, `Expression` |
| `ActionExpectation` | `Action`, `Expression` |
| `CollectionExpectation<T>` | `Actual`, `Expression` |

**Best Practices:**
- Include `exp.Expression` in error messages for clear failure output
- Throw `AssertionException` for assertion failures
- Use descriptive method names following the `toBe...` / `toHave...` pattern

## Hook Execution Order

Hooks execute in a specific order to ensure proper setup/teardown:

```
describe("Outer", () => {
    beforeAll(() => console.log("1. outer beforeAll"));
    beforeEach(() => console.log("3. outer beforeEach"));
    afterEach(() => console.log("5. outer afterEach"));
    afterAll(() => console.log("7. outer afterAll"));

    describe("Inner", () => {
        beforeAll(() => console.log("2. inner beforeAll"));
        beforeEach(() => console.log("4. inner beforeEach"));
        afterEach(() => console.log("6. inner afterEach"));
        afterAll(() => console.log("8. inner afterAll"));

        it("spec", () => { });
    });
});
```

**Order:**
1. BeforeAll: root → leaf (once per context)
2. BeforeEach: root → leaf (per spec)
3. Spec body
4. AfterEach: leaf → root (per spec)
5. AfterAll: leaf → root (once per context)

## Data Flow

```
User Code
    │
    ▼ describe/it/expect calls
DSL Layer
    │
    ▼ builds SpecContext tree
Core Domain
    │
    ▼ passed to runner
Execution Layer
    │
    ├──► Middleware Pipeline
    │         │
    │         ▼ wraps each spec
    │    Spec Body
    │
    ▼ produces results
SpecReport
    │
    ├──► IReporter callbacks (during execution)
    │
    ▼ formatted output
IFormatter.Format()
    │
    ▼
JSON / HTML / Markdown
```

## Dependency Injection

DraftSpec uses `Microsoft.Extensions.DependencyInjection` throughout for consistent service registration and resolution across all layers.

### Configuration API

`DraftSpecConfiguration` exposes the full MS.DI API:

```csharp
var config = new DraftSpecConfiguration();

// Register services using standard MS.DI patterns
config.Services.AddSingleton<IMyService, MyService>();
config.Services.AddTransient<IFactory, Factory>();

// Convenience method for singleton instances
config.AddService(new MyService());

// Resolve services
var service = config.ServiceProvider.GetService<IMyService>();
```

### Service Lifetimes

| Lifetime | Registration | Behavior |
|----------|--------------|----------|
| Singleton | `AddSingleton<T>()` | Single instance for entire configuration |
| Transient | `AddTransient<T>()` | New instance per resolution |
| Scoped | `AddScoped<T>()` | New instance per scope |

### Plugin Service Access

Plugins access services via `IPluginContext`:

```csharp
public class MyPlugin : IPlugin
{
    public void Initialize(IPluginContext context)
    {
        var service = context.GetService<IMyService>();
        var required = context.GetRequiredService<ILogger>();
    }
}
```

### Unified Across Projects

| Project | DI Approach |
|---------|-------------|
| DraftSpec (core) | `DraftSpecConfiguration.Services` (IServiceCollection) |
| DraftSpec.Cli | `ServiceCollectionExtensions.AddDraftSpec()` |
| DraftSpec.Mcp | `ServiceCollectionExtensions.AddDraftSpecMcp()` |

All projects use the same `Microsoft.Extensions.DependencyInjection` patterns.

## Key Design Decisions

See [Architecture Decision Records](./adr/) for detailed rationale:

- **ADR-001:** RSpec-style DSL over attribute-based tests
- **ADR-002:** Callback-based IReporter for streaming output
- **ADR-003:** Middleware pipeline for cross-cutting concerns
- **ADR-004:** Interface-based plugins for extensibility
- **ADR-005:** Pre-1.0 breaking changes policy
- **ADR-007:** Declarative DSL model (scripts define specs, framework controls execution)

## Project Structure

```
src/
  DraftSpec/                      # Core library
    Dsl.cs                        # Static DSL entry point (partial)
    Spec.cs                       # Class-based alternative
    SpecContext.cs                # describe/context block
    SpecDefinition.cs             # it block
    SpecResult.cs                 # Execution result
    SpecRunner.cs                 # Tree walker
    SpecRunnerBuilder.cs          # Fluent configuration
    Expectations/                 # Assertion matchers
    Middleware/                   # Pipeline components
    Plugins/                      # Extension interfaces
    Configuration/                # Runtime configuration
    Internal/                     # Implementation details
  DraftSpec.Cli/                  # Command-line tool
  DraftSpec.Formatters.Html/      # HTML output
  DraftSpec.Formatters.Markdown/  # Markdown output
```
