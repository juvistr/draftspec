# DraftSpec

RSpec-inspired testing for .NET 10.

DraftSpec brings the elegant `describe`/`it`/`expect` syntax to .NET, filling the gap left by abandoned BDD frameworks like NSpec. Write expressive specs in CSX scripts or traditional test classes.

## Quick Start

**1. Install dotnet-script:**
```bash
dotnet tool install -g dotnet-script
```

**2. Create a spec file** (`Calculator.spec.csx`):
```csharp
#r "nuget: DraftSpec, *"
using static DraftSpec.Dsl;

describe("Calculator", () =>
{
    it("adds numbers", () =>
    {
        expect(1 + 1).toBe(2);
    });

    it("handles negatives", () =>
    {
        expect(-1 + -1).toBe(-2);
    });
});

run();
```

**3. Run it:**
```bash
dotnet script Calculator.spec.csx
```

**Output:**
```
Calculator
  ✓ adds numbers
  ✓ handles negatives

2 passed
```

## Features

### Nested Contexts
```csharp
describe("User", () =>
{
    describe("when logged in", () =>
    {
        it("can view dashboard", () => { /* ... */ });
        it("can update profile", () => { /* ... */ });
    });

    describe("when logged out", () =>
    {
        it("redirects to login", () => { /* ... */ });
    });
});
```

### Hooks
```csharp
describe("Database", () =>
{
    beforeAll(() => { /* open connection once */ });
    afterAll(() => { /* close connection */ });

    before(() => { /* begin transaction */ });
    after(() => { /* rollback transaction */ });

    it("inserts record", () => { /* ... */ });
});
```

### Async Support
```csharp
it("fetches data", async () =>
{
    var result = await httpClient.GetAsync("/api/users");
    expect(result.StatusCode).toBe(HttpStatusCode.OK);
});
```

### Focus & Skip
```csharp
fit("only this runs", () => { });     // Focus: only focused specs run
xit("this is skipped", () => { });    // Skip: explicitly disabled
it("this is pending");                 // Pending: no body = pending
```

### Assertions
```csharp
// Equality
expect(value).toBe(expected);
expect(value).toBeNull();
expect(value).toNotBeNull();

// Numbers
expect(count).toBeGreaterThan(0);
expect(count).toBeLessThan(100);
expect(price).toBeCloseTo(19.99, 0.01);

// Strings
expect(name).toContain("Smith");
expect(email).toStartWith("user@");
expect(path).toEndWith(".txt");

// Booleans
expect(isValid).toBeTrue();
expect(isEmpty).toBeFalse();

// Collections
expect(items).toContain("apple");
expect(items).toHaveCount(3);
expect(items).toBeEmpty();

// Exceptions
expect(() => Divide(1, 0)).toThrow<DivideByZeroException>();
expect(() => SafeOperation()).toNotThrow();
```

### CLI Tool
```bash
# Run specs
draftspec run .                       # Run all *.spec.csx in current directory
draftspec run ./specs                 # Run specs in directory
draftspec run MySpec.spec.csx         # Run single file

# Output formats
draftspec run . --format json         # JSON output
draftspec run . --format html -o report.html

# Watch mode
draftspec watch .                     # Re-run on file changes

# Parallel execution
draftspec run . --parallel
```

### Middleware & Plugins
```csharp
configure(runner => runner
    .WithRetry(3)                     // Retry failed specs
    .WithTimeout(5000)                // 5 second timeout
    .WithParallelExecution()          // Run specs in parallel
);
```

## Documentation

- **[Getting Started](docs/getting-started.md)** - Installation and first spec
- **[DSL Reference](docs/dsl-reference.md)** - Complete API for describe/it/hooks
- **[Assertions](docs/assertions.md)** - Full expect() API reference
- **[CLI Reference](docs/cli.md)** - Command-line options
- **[Configuration](docs/configuration.md)** - Middleware and plugins

## Status

DraftSpec is pre-1.0. The API is stabilizing but breaking changes may occur in minor versions. See [ADR-005](docs/adr/005-breaking-changes.md) for versioning policy.

## License

MIT
