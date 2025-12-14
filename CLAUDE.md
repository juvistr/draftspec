# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DraftSpec is an RSpec-inspired testing framework for .NET 10, filling the gap left by abandoned frameworks like NSpec.

## Build and Test Commands

```bash
dotnet build                                          # Build all projects
dotnet run --project src/DraftSpec.Scratchpad         # Run DSL scratchpad
dotnet run --project tests/DraftSpec.Tests            # Run TUnit tests
```

## Project Structure

```
DraftSpec.sln
src/
  DraftSpec/                      # Core library
    Spec.cs                       # Base class: describe/context/it/fit/xit, hooks
    SpecContext.cs                # Nested block with children, specs, hooks
    SpecDefinition.cs             # Single spec: description, body, flags
    SpecResult.cs                 # Execution result with ContextPath
    SpecRunner.cs                 # Tree walker, executes specs
  DraftSpec.Scratchpad/           # Console app for DSL experimentation
    PatientRecordSpec.cs          # Example spec
    Program.cs                    # Hierarchical output renderer
tests/
  DraftSpec.Tests/                # TUnit tests for internals
    HookOrderingTests.cs          # Verifies before/after hook order
    FocusModeTests.cs             # Verifies fit/xit behavior
```

## Current DSL

```csharp
public class MySpec : Spec
{
    public MySpec()
    {
        describe("feature", () =>
        {
            before = () => { /* runs before each spec */ };
            after = () => { /* runs after each spec */ };
            beforeAll = () => { /* runs once before all specs in context */ };
            afterAll = () => { /* runs once after all specs in context */ };

            it("does something", () => { /* assertion */ });
            it("pending spec");                              // no body = pending
            fit("focused", () => { });                       // only focused specs run
            xit("skipped", () => { });                       // explicitly skipped
        });
    }
}
```

## Key Behaviors

- **Hook order**: beforeAll → beforeEach (parent→child) → spec → afterEach (child→parent) → afterAll
- **Focus mode**: Any `fit` causes all non-focused specs to skip
- **Pending**: `it("description")` without body marks spec as pending

## Research

See `docs/research/INITIAL.md` for analysis of C# 14 features and framework landscape.
