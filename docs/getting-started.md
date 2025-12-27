# Getting Started with DraftSpec

This guide walks you through setting up DraftSpec and writing your first spec.

## Prerequisites

- **.NET 10 SDK** or later
- **dotnet-script** tool for running CSX files

## Installation

### 1. Install dotnet-script

DraftSpec specs are written as CSX (C# Script) files. Install the dotnet-script tool:

```bash
dotnet tool install -g dotnet-script
```

### 2. Install the DraftSpec CLI (Optional)

The CLI provides convenient commands for running specs, watch mode, and project initialization:

```bash
dotnet tool install -g DraftSpec.Cli --prerelease
```

## Your First Spec

### Option A: Quick Start (Single File)

Create a file called `example.spec.csx`:

```csharp
#r "nuget: DraftSpec"
using static DraftSpec.Dsl;

describe("Math", () =>
{
    it("adds positive numbers", () =>
    {
        expect(2 + 2).toBe(4);
    });

    it("adds negative numbers", () =>
    {
        expect(-1 + -1).toBe(-2);
    });

    it("multiplies", () =>
    {
        expect(3 * 4).toBe(12);
    });
});

run();
```

Run it:

```bash
dotnet script example.spec.csx
```

Output:
```
Math
  ✓ adds positive numbers
  ✓ adds negative numbers
  ✓ multiplies

3 passed
```

### Option B: Project Setup (Recommended)

For testing a real project, use the CLI to set up the infrastructure:

```bash
# Navigate to your project directory
cd MyProject

# Initialize DraftSpec
draftspec init

# This creates:
#   spec_helper.csx   - Shared references and fixtures
#   omnisharp.json    - IDE support configuration
```

Create your first spec file, `MyClass.spec.csx`:

```csharp
#load "spec_helper.csx"
using MyProject;

var calculator = new Calculator();

describe("Calculator", () =>
{
    describe("Add", () =>
    {
        it("returns 0 for empty input", () =>
        {
            expect(calculator.Add("")).toBe(0);
        });

        it("returns the number for single input", () =>
        {
            expect(calculator.Add("5")).toBe(5);
        });

        it("sums multiple numbers", () =>
        {
            expect(calculator.Add("1,2,3")).toBe(6);
        });
    });
});

run();
```

Run with the CLI:

```bash
draftspec run .
```

## Project Structure

Recommended layout for a project with specs:

```
MyProject/
├── MyProject.csproj
├── src/
│   └── Calculator.cs
├── spec_helper.csx          # Shared references
├── omnisharp.json           # IDE configuration
├── Calculator.spec.csx      # Specs alongside source
└── specs/                   # Or in a separate folder
    ├── Feature1.spec.csx
    └── Feature2.spec.csx
```

### spec_helper.csx

The spec helper centralizes references and shared fixtures:

```csharp
#r "nuget: DraftSpec"
#r "bin/Debug/net10.0/MyProject.dll"

using static DraftSpec.Dsl;

// Shared fixtures
public class TestDatabase
{
    public void Setup() { /* ... */ }
    public void Teardown() { /* ... */ }
}

public static TestDatabase Db = new();
```

Spec files load the helper:

```csharp
#load "spec_helper.csx"

describe("Feature", () =>
{
    before(() => Db.Setup());
    after(() => Db.Teardown());

    it("uses shared fixture", () => { /* ... */ });
});

run();
```

### File Naming

- Spec files: `*.spec.csx`
- The CLI discovers all `*.spec.csx` files in the directory tree

## Running Specs

### With dotnet-script

```bash
dotnet script MySpec.spec.csx
```

### With the CLI

```bash
# Run all specs in current directory
draftspec run .

# Run specific file
draftspec run Calculator.spec.csx

# Run specs in a folder
draftspec run ./specs

# Watch mode - re-run on changes
draftspec watch .

# Parallel execution
draftspec run . --parallel

# Output to file
draftspec run . --format html -o report.html
```

## IDE Support

### VS Code

1. Install the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension
2. The `omnisharp.json` created by `draftspec init` enables IntelliSense for CSX files

### JetBrains Rider

Rider has built-in support for CSX files. The `omnisharp.json` configuration helps with NuGet resolution.

## Next Steps

- **[DSL Reference](dsl-reference.md)** - Full API for describe, it, hooks, and more
- **[Assertions](assertions.md)** - Complete expect() API
- **[CLI Reference](cli.md)** - All command-line options
- **[Configuration](configuration.md)** - Middleware, plugins, and customization
