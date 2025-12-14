# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DraftSpec is an RSpec-inspired testing framework for .NET 10, filling the gap left by abandoned frameworks like NSpec. Supports both CSX scripts (recommended) and class-based specs.

## Build and Test Commands

```bash
dotnet build                                          # Build all projects
dotnet run --project tests/DraftSpec.Tests            # Run TUnit tests

# Run CSX specs
cd examples/Calculator && dotnet script Calculator.spec.csx
```

## Project Structure

```
DraftSpec.sln
src/
  DraftSpec/                      # Core library
    Dsl.cs                        # Static DSL for CSX: describe/it/expect
    Spec.cs                       # Class-based DSL (alternative)
    Expect.cs                     # Jest-style assertions
    AssertionException.cs         # Assertion failure exception
    SpecContext.cs                # Nested block with children, specs, hooks
    SpecDefinition.cs             # Single spec: description, body, flags
    SpecResult.cs                 # Execution result with ContextPath
    SpecRunner.cs                 # Tree walker, executes specs
examples/
  Calculator/                     # Example project with CSX specs
    Calculator.cs                 # StringCalculator implementation
    Calculator.spec.csx           # Specs using expect() API
tests/
  DraftSpec.Tests/                # TUnit tests for internals
```

## CSX Specs (Recommended)

```csharp
// Calculator.spec.csx
#r "path/to/DraftSpec.dll"
#r "bin/Debug/net10.0/MyProject.dll"
using static DraftSpec.Dsl;

describe("Calculator", () =>
{
    before(() => { /* runs before each spec */ });
    after(() => { /* runs after each spec */ });

    it("adds numbers", () =>
    {
        expect(calc.Add("1,2")).toBe(3);
    });

    it("pending spec");  // no body = pending

    fit("focused", () => { });  // only focused specs run
    xit("skipped", () => { });  // explicitly skipped
});

run();
```

Run with: `dotnet script Calculator.spec.csx`

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
expect(() => action()).toThrow<T>();    // exceptions
expect(() => action()).toNotThrow();
```

Failures auto-capture the expression:
```
Expected calc.Add("1,2") to be 5, but was 3
```

## Key Behaviors

- **Hook order**: beforeAll → before (parent→child) → spec → after (child→parent) → afterAll
- **Focus mode**: Any `fit` causes all non-focused specs to skip
- **Pending**: `it("description")` without body marks spec as pending

## Research

- `docs/research/INITIAL.md` - C# 14 features and framework landscape
- `docs/research/FILE_STRUCTURE.md` - CSX constraints and alternatives
