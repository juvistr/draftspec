# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DraftSpec is an RSpec-inspired BDD testing framework for .NET 10, filling the gap left by abandoned frameworks like NSpec. Run specs via the CLI tool (`draftspec run`) or `dotnet test` with MTP integration.

## Build and Test Commands

```bash
dotnet build                                          # Build all projects
dotnet run --project tests/DraftSpec.Tests            # Run TUnit tests

# Run specs via CLI
draftspec run examples/TodoApi/Specs                  # Run all specs in directory
draftspec run examples/TodoApi/Specs/TodoService.spec.csx  # Run single file
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
  DraftSpec.Cli/                  # CLI tool: draftspec run/watch/list
  DraftSpec.TestingPlatform/      # MTP adapter for dotnet test integration
  DraftSpec.Mcp/                  # MCP server for AI-assisted testing
  DraftSpec.Formatters.*/         # Output formatters (Console, Html, Markdown, Json)
examples/
  TodoApi/                        # Comprehensive example project
    spec_helper.csx               # Shared setup + fixtures
    Specs/
      features_showcase.spec.csx  # Systematic feature demo
      TodoService.spec.csx        # Real-world usage patterns
      async_specs.spec.csx        # Async/await patterns
      hooks_demo.spec.csx         # Hook execution order
tests/
  DraftSpec.Tests/                # TUnit tests for internals
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

## CI Commands

```bash
gh run list --limit 5                    # List recent workflow runs
gh run watch <run-id> --exit-status      # Watch a run until completion
gh pr checks <pr-number>                 # Check PR status
gh pr checks <pr-number> --watch         # Watch PR checks until completion
```

## Research

- `docs/research/INITIAL.md` - C# 14 features and framework landscape
- `docs/research/FILE_STRUCTURE.md` - CSX constraints and alternatives
