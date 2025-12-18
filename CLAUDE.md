# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DraftSpec is an RSpec-inspired testing framework for .NET 10, filling the gap left by abandoned frameworks like NSpec. Supports both CSX scripts (recommended) and class-based specs.

## Build and Test Commands

```bash
dotnet build                                          # Build all projects
dotnet run --project tests/DraftSpec.Tests            # Run TUnit tests

# Run CSX specs
cd examples/TodoApi && dotnet script Specs/features_showcase.spec.csx
```

## Project Structure

```
DraftSpec.sln
src/
  DraftSpec/                      # Core library
    Dsl.cs                        # Static DSL for CSX: describe/it/expect
    Spec.cs                       # Class-based DSL (alternative)
    Expectations/                 # Jest-style assertions (Expectation<T>, etc.)
    SpecContext.cs                # Nested block with children, specs, hooks
    SpecDefinition.cs             # Single spec: description, body, flags
    SpecResult.cs                 # Execution result with ContextPath
    SpecRunner.cs                 # Tree walker, executes specs
  DraftSpec.Cli/                  # CLI tool (dotnet tool)
  DraftSpec.Formatters.*/         # Output formatters (Console, Html, Markdown)
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

## CSX Specs (Recommended)

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

run();
```

Run with: `dotnet script Specs/TodoService.spec.csx`

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
3. Open PR referencing the issue (e.g., "Closes #63")
4. Squash merge via PR (merge commits disabled)

## Research

- `docs/research/INITIAL.md` - C# 14 features and framework landscape
- `docs/research/FILE_STRUCTURE.md` - CSX constraints and alternatives
