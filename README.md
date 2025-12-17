# DraftSpec

[![CI](https://github.com/juvistr/draftspec/actions/workflows/ci.yml/badge.svg)](https://github.com/juvistr/draftspec/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/juvistr/draftspec/graph/badge.svg)](https://codecov.io/gh/juvistr/draftspec)

> ⚠️ **Alpha** - Experimental, expect breaking changes

RSpec-inspired testing for .NET.

DraftSpec brings the elegant `describe`/`it`/`expect` syntax to .NET, filling the gap left by abandoned BDD frameworks
like NSpec. Write expressive specs as scripts or traditional test classes.

> **Requires .NET 8+** for CSX scripts via dotnet-script, or **.NET 10+** for file-based apps.

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

### MCP Server (AI Integration)

DraftSpec includes an MCP server for AI-assisted testing:

```bash
# Add to Claude Desktop, VS Code, etc.
dotnet run --project src/DraftSpec.Mcp
```

> **Security Note:** The MCP server executes arbitrary code from spec content via `dotnet run`. Only use in trusted
> environments or within sandboxed containers with restricted permissions.

**Tools:**

| Tool | Description |
|------|-------------|
| `run_spec` | Execute spec code and return structured JSON results |
| `scaffold_specs` | Generate pending specs from a structured description |

**scaffold_specs** - Generate spec scaffolds for BDD (behavior-first) or characterisation (code-first) workflows:

```json
{
  "description": "UserService",
  "contexts": [
    { "description": "Create", "specs": ["creates user", "hashes password"] },
    { "description": "GetById", "specs": ["returns user when found", "returns null when not found"] }
  ]
}
```

Output:
```csharp
describe("UserService", () =>
{
    describe("Create", () =>
    {
        it("creates user");
        it("hashes password");
    });

    describe("GetById", () =>
    {
        it("returns user when found");
        it("returns null when not found");
    });
});
```

Agents can scaffold specs, then fill in assertions using `run_spec` to verify.

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

## Contributing

We use a PR-based workflow with branch protection on `main`.

### Development

```bash
# Clone and build
git clone https://github.com/juvistr/draftspec.git
cd draftspec
dotnet build

# Run tests
dotnet run --project tests/DraftSpec.Tests

# Create a branch for your changes
git checkout -b feature/your-feature
```

### Pull Request Process

1. Create a feature branch from `main`
2. Make your changes with tests
3. Push and open a PR
4. CI runs automatically (build, test, pack)
5. After review and approval, squash-merge to `main`

### Releases

Releases are triggered by pushing version tags:

```bash
git tag v0.1.0-alpha.1
git push origin v0.1.0-alpha.1
```

This triggers the publish workflow which:
- Builds and tests the code
- Packs NuGet packages (version from tag via MinVer)
- Publishes to nuget.org
- Creates a GitHub Release with auto-generated notes

### Branch Protection (Maintainers)

Recommended settings for `main`:
- Require PR reviews before merging
- Require status checks (CI workflow)
- Require linear history (squash merging)
- No direct pushes to main

## Status

**Alpha** - This is an early experiment, built in roughly a day with AI assistance. It works, but expect rough edges,
missing features, and breaking changes.

If you try it and hit issues, feedback is welcome via GitHub issues.

## License

[MIT](LICENSE)
