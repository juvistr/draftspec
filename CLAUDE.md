# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DraftSpec is an RSpec-inspired BDD testing framework for .NET 10, filling the gap left by abandoned frameworks like NSpec. Run specs via the CLI tool (`draftspec run`) or `dotnet test` with MTP integration.

## Build and Test Commands

```bash
dotnet build                                          # Build all projects
dotnet run --project tests/DraftSpec.Tests            # Run unit tests
dotnet run --project tests/DraftSpec.Tests -- --treenode-filter '/*/*/SpecRunnerTests/*'  # Filter by class
dotnet run --project tests/DraftSpec.Cli.IntegrationTests  # Run CLI integration tests

# Run specs via CLI
draftspec run examples/TodoApi/Specs                  # Run all specs in directory
draftspec run examples/TodoApi/Specs/TodoService.spec.csx  # Run single file
draftspec run . --interactive                         # Interactive spec selection
draftspec list .                                      # List specs without running
draftspec validate .                                  # Validate spec files
draftspec docs . --output docs/specs                  # Generate living documentation
draftspec coverage-map .                              # Map specs to source methods
```

## Project Structure

```
DraftSpec.sln
src/
  DraftSpec/                      # Core library
    Dsl.cs                        # Static DSL: describe/it/expect
    Spec.cs                       # Class-based DSL (alternative)
    Expectations/                 # Jest-style assertions (Expectation<T>, etc.)
    SpecContext.cs                # Nested block with children, specs, hooks
    SpecDefinition.cs             # Single spec: description, body, flags
    SpecResult.cs                 # Execution result with ContextPath
    SpecRunner.cs                 # Tree walker, executes specs
  DraftSpec.Cli/                  # CLI tool (12 commands)
  DraftSpec.Scripting/            # Roslyn-based CSX script compilation
  DraftSpec.TestingPlatform/      # MTP adapter for dotnet test integration
  DraftSpec.Mcp/                  # MCP server for AI-assisted testing
  DraftSpec.Formatters.Abstractions/  # IFormatter interface
  DraftSpec.Formatters.Console/   # Terminal output
  DraftSpec.Formatters.Html/      # HTML reports
  DraftSpec.Formatters.Markdown/  # Markdown reports
  DraftSpec.Formatters.JUnit/     # JUnit XML for CI/CD
examples/
  TodoApi/                        # Example API project
  TodoApi.Specs/                  # Specs for TodoApi (MTP integration)
tests/
  DraftSpec.Tests/                # Unit tests for internals
  DraftSpec.Cli.IntegrationTests/ # CLI integration tests
```

## Spec File Format

```csharp
// TodoService.spec.csx
#load "../spec_helper.csx"
using static DraftSpec.Dsl;

describe("TodoService", () =>
{
    before(() => { service = CreateService(); });

    it("creates a todo", async () =>
    {
        var todo = await service.CreateAsync("Buy milk");
        expect(todo.Title).toBe("Buy milk");
    });

    it("pending spec");  // no body = pending

    fit("focused", () => { });  // only focused specs run
    xit("skipped", () => { });  // explicitly skipped
});
```

Run with: `draftspec run TodoService.spec.csx`

## Assertions (expect API)

```csharp
expect(value).toBe(expected);           // equality
expect(value).toBeNull();               // null check
expect(value).toNotBeNull();
expect(flag).toBeTrue();                // boolean
expect(flag).toBeFalse();
expect(str).toContain("substring");     // string
expect(str).toStartWith("prefix");
expect(str).toEndWith("suffix");
expect(items).toContain(item);          // collections
expect(items).toHaveCount(3);
expect(() => action()).toThrow<T>();    // exceptions
expect(() => action()).toNotThrow();
```

Failures auto-capture the expression:
```
Expected todo.Title to be "Buy milk", but was "Buy bread"
```

## Key Behaviors

- **Hook order**: beforeAll → before (parent→child) → spec → after (child→parent) → afterAll
- **Multiple hooks**: Each context supports multiple hooks via `context.AddBeforeEach()`, `AddAfterEach()`, `AddBeforeAll()`, `AddAfterAll()`. Before hooks run FIFO, after hooks run LIFO.
- **Focus mode**: Any `fit` causes all non-focused specs to skip
- **Pending**: `it("description")` without body marks spec as pending

## Git Workflow

**Branch naming** (semantic prefixes):
- `feat/<short-description>` - New features
- `fix/<short-description>` - Bug fixes
- `test/<short-description>` - Test additions
- `docs/<short-description>` - Documentation
- `refactor/<short-description>` - Code refactoring

**Workflow**:
1. Never push directly to `main` (branch protection enforced)
2. Create feature branch from `main`
3. Open PR with issue reference in body to auto-close
4. Squash merge via PR (merge commits disabled)

**Auto-closing issues**: Include one of these keywords followed by the issue number in the PR body (not title) to auto-close when merged:
- `Closes #123` or `Fixes #123` (preferred)
- `Resolves #123`

Example PR body:
```
## Summary
Add table-driven test support with withData method.

Closes #72
```

## PR Title Format (Conventional Commits)

**IMPORTANT**: Use conventional commit format for PR titles. This enables automatic labeling and categorized release notes.

```
<type>(<scope>): <description>
```

| Type | Label | When to Use |
|------|-------|-------------|
| `feat` | `feature` | New functionality |
| `fix` | `fix` | Bug fixes |
| `perf` | `performance` | Performance improvements |
| `security` | `security` | Security fixes |
| `docs` | `documentation` | Documentation only |
| `test` | `test` | Test additions/changes |
| `refactor` | `refactor` | Code refactoring |
| `chore` | `chore` | Build, CI, dependencies |
| `ci` | `chore` | CI/CD changes |

**Breaking changes**: Add `!` after the type (e.g., `feat!:` or `fix(auth)!:`)

**Common scopes**: `cli`, `runner`, `dsl`, `mtp`, `mcp`, `scripting`, `formatters`, `deps`

Examples:
```
feat(cli): add watch mode for continuous testing
fix(runner): handle async disposal correctly
perf(cache): eliminate redundant file hashing
refactor(scripting): extract common cache base class
```

## CI Commands

```bash
gh run list --limit 5                    # List recent workflow runs
gh run watch <run-id> --exit-status      # Watch a run until completion
gh pr checks <pr-number>                 # Check PR status
gh pr checks <pr-number> --watch         # Watch PR checks until completion
```

## Code Style

```bash
dotnet format                     # Auto-format code
```

## Dependencies

**Be deliberate about adding dependencies.** Before introducing a new NuGet package:
1. Check if existing code already solves the problem
2. Consider if a simple implementation is preferable to a dependency
3. Discuss with the user before adding new packages

### DSL References in Tests

Tests in `DraftSpec.Tests.*` namespaces that reference `Dsl` will resolve to `DraftSpec.Tests.Dsl` (a test folder namespace), not `DraftSpec.Dsl`. Use `using static DraftSpec.Dsl;` to import the static DSL methods directly.

### Testing Patterns

This project uses **handwritten mocks** instead of mocking frameworks (no NSubstitute, Moq, etc.). This is intentional:

- Mock classes live in `tests/DraftSpec.Tests/Infrastructure/Mocks/`
- Examples: `MockConsole`, `MockFileSystem`, `MockSpecDiscoverer`
- Pattern: Fluent configuration with `.With*()` methods and call tracking via `*Calls` lists

```csharp
// Example: Using existing mocks
var discoverer = new MockSpecDiscoverer()
    .WithSpecs(spec1, spec2)
    .WithErrors(error1);

// Verify calls
await Assert.That(discoverer.DiscoverAsyncCalls).HasCount().EqualTo(1);
```

Before creating a new mock, check `Infrastructure/Mocks/` for existing implementations.

### Cross-Platform Paths in Tests

**IMPORTANT**: Use `TestPaths` helper for file paths in tests to ensure they work on Windows, macOS, and Linux.

```csharp
using DraftSpec.Tests.Infrastructure;

// Instead of: "/project/specs/test.spec.csx" (fails on Windows)
var path = TestPaths.Project("specs/test.spec.csx");

// Available helpers:
TestPaths.ProjectDir          // Cross-platform fake project directory
TestPaths.Project("file.csx") // Path within project dir
TestPaths.Spec("test.csx")    // Path within specs dir
TestPaths.Temp("file.txt")    // Path within temp dir
TestPaths.Coverage("cov.xml") // Path within coverage dir
```

The helper lives in `tests/DraftSpec.Tests/Infrastructure/TestPaths.cs`.

### Code Coverage

**Target near-100% coverage on new code.** Before opening a PR:
1. Add comprehensive unit tests for all new classes and methods
2. Cover edge cases (null inputs, empty collections, error conditions)
3. Test both success and failure paths
4. Use descriptive test names that explain the expected behavior

## Architecture

See `ARCHITECTURE.md` for deep dives on:
- Middleware pipeline (ASP.NET Core-style) for spec execution
- AsyncLocal thread safety in the static DSL
- Plugin system (IFormatter, IReporter, ISpecMiddleware)