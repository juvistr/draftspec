# DraftSpec

[![CI](https://github.com/juvistr/draftspec/actions/workflows/ci.yml/badge.svg)](https://github.com/juvistr/draftspec/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/juvistr/draftspec/graph/badge.svg)](https://codecov.io/gh/juvistr/draftspec)
[![NuGet](https://img.shields.io/nuget/v/DraftSpec.svg)](https://www.nuget.org/packages/DraftSpec)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> **Alpha** - API stabilizing, expect some breaking changes

RSpec-inspired BDD testing framework for .NET 10.

DraftSpec brings the elegant `describe`/`it`/`expect` syntax to .NET, filling the gap left by abandoned BDD frameworks like NSpec.

## Requirements

- **.NET 10 SDK**

## Quick Start

### Option 1: CLI Tool

**1. Install the CLI:**

```bash
dotnet tool install -g DraftSpec.Cli --prerelease
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
```

**3. Run it:**

```bash
draftspec run Calculator.spec.csx
```

**Output:**

```
Calculator
  adds numbers
  handles negatives

2 passed
```

### Option 2: dotnet test Integration

Run specs via `dotnet test` with full IDE Test Explorer support:

```bash
# Add package to your test project
dotnet add package DraftSpec.TestingPlatform --prerelease

# Add spec files (*.spec.csx) to your project
# Run tests
dotnet test
```

Features:
- Visual Studio / VS Code / Rider Test Explorer integration
- Click-to-navigate from test results to source
- Built-in code coverage collection
- Standard `dotnet test` CI/CD integration

See [MTP Integration Guide](docs/mtp-integration.md) for full documentation.

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

Hook execution order: `beforeAll` -> `before` (parent to child) -> spec -> `after` (child to parent) -> `afterAll`

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

## CLI Reference

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

# Parallel execution and filtering
draftspec run . --parallel            # Run spec files in parallel
draftspec run . --tags unit,fast      # Only run specs with these tags
draftspec run . --exclude-tags slow   # Exclude specs with these tags
draftspec run . --bail                # Stop on first failure
```

### Configuration File

Create a `draftspec.json` in your project for persistent settings:

```json
{
  "parallel": true,
  "timeout": 10000,
  "bail": false,
  "tags": {
    "include": ["unit", "fast"],
    "exclude": ["slow", "integration"]
  },
  "reporters": ["console", "json"],
  "noCache": false
}
```

CLI options override config file values.

## Output Formats

- **Console** - Human-readable output (default)
- **JSON** - Structured results for tooling
- **HTML** - Visual report for browsers
- **Markdown** - For documentation and GitHub
- **JUnit** - For CI/CD integration

## MCP Server (AI Integration)

DraftSpec includes an MCP server for AI-assisted testing workflows:

```bash
dotnet run --project src/DraftSpec.Mcp
```

> **Security Warning:** The MCP server executes arbitrary C# code with full process privileges. See [SECURITY.md](SECURITY.md) for deployment recommendations and container isolation guidance.

**Tools:**

| Tool | Description |
|------|-------------|
| `run_spec` | Execute spec code and return structured JSON results |
| `scaffold_specs` | Generate pending specs from a structured description |
| `parse_assertion` | Convert natural language to `expect()` syntax |

See [MCP documentation](docs/mcp.md) for detailed usage.

## Documentation

- **[Getting Started](docs/getting-started.md)** - Installation and first spec
- **[DSL Reference](docs/dsl-reference.md)** - Complete API for describe/it/hooks
- **[Assertions](docs/assertions.md)** - Full expect() API reference
- **[CLI Reference](docs/cli.md)** - Command-line options
- **[MTP Integration](docs/mtp-integration.md)** - dotnet test and IDE integration
- **[Configuration](docs/configuration.md)** - Settings and customization

## Status

**Alpha (v0.4.x)** - Core functionality is stable with 2000+ tests and 80%+ code coverage. API is stabilizing but may have breaking changes before v1.0.

## Contributing

We use a PR-based workflow with branch protection on `main`.

```bash
# Clone and build
git clone https://github.com/juvistr/draftspec.git
cd draftspec
dotnet build

# Run tests
dotnet run --project tests/DraftSpec.Tests

# Create a branch for your changes
git checkout -b feat/your-feature
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for the full development guide.

## License

[MIT](LICENSE)
