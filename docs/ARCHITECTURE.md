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
run();
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
var output = FormatterRegistry.Get("xml").Format(report);
```

### 3. Custom Reporters

Implement `IReporter` and add to configuration:

```csharp
var config = new DraftSpecConfiguration();
config.AddReporter(new SlackReporter());
```

### 4. Custom Assertions

Extend expectation classes or create custom matchers:

```csharp
public static class DateExpectationExtensions
{
    public static void toBeWithin(this Expectation<DateTime> exp, TimeSpan tolerance, DateTime expected)
    {
        var diff = Math.Abs((exp.Actual - expected).TotalMilliseconds);
        if (diff > tolerance.TotalMilliseconds)
            throw new AssertionException($"Expected within {tolerance} of {expected}");
    }
}
```

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

## Key Design Decisions

See [Architecture Decision Records](./adr/) for detailed rationale:

- **ADR-001:** RSpec-style DSL over attribute-based tests
- **ADR-002:** Callback-based IReporter for streaming output
- **ADR-003:** Middleware pipeline for cross-cutting concerns
- **ADR-004:** Interface-based plugins for extensibility
- **ADR-005:** Pre-1.0 breaking changes policy

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
