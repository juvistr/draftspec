# Microsoft Testing Platform Integration

DraftSpec integrates with Microsoft Testing Platform (MTP), enabling:

- `dotnet test` execution of CSX specs
- Built-in code coverage via `--coverage`
- Visual Studio / VS Code / Rider Test Explorer integration
- Click-to-navigate from test results to source code

## Quick Start

### 1. Create a Test Project

```bash
mkdir MyProject.Specs
cd MyProject.Specs
dotnet new classlib
```

### 2. Add Dependencies

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="DraftSpec.TestingPlatform" Version="*" />
    <!-- Reference your project under test -->
    <ProjectReference Include="..\MyProject\MyProject.csproj" />
  </ItemGroup>
</Project>
```

### 3. Create Spec Files

Create `.spec.csx` files in your test project:

```csharp
// Calculator.spec.csx
using static DraftSpec.Dsl;

describe("Calculator", () =>
{
    it("adds numbers", () =>
    {
        expect(1 + 1).toBe(2);
    });

    it("subtracts numbers", () =>
    {
        expect(5 - 3).toBe(2);
    });
});
```

### 4. Run Tests

```bash
dotnet test
```

## Running Tests

### Basic Execution

```bash
# Run all specs
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test by filter
dotnet test --filter "Calculator"
```

### Code Coverage

```bash
# Run with coverage collection
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

### Parallel Execution

```bash
# Run in parallel (default for MTP)
dotnet test

# Disable parallelism
dotnet test -- --parallel-mode none
```

## IDE Integration

### Visual Studio

1. Open your solution in Visual Studio 2022+
2. Build the solution
3. Open Test Explorer (Test > Test Explorer)
4. DraftSpec specs appear under their describe/it hierarchy
5. Click any test to navigate to source

### Visual Studio Code

1. Install the [.NET Core Test Explorer](https://marketplace.visualstudio.com/items?itemName=formulahendry.dotnet-test-explorer) extension
2. Open the folder containing your solution
3. Tests appear in the Test Explorer sidebar
4. Click to run or debug individual specs

### JetBrains Rider

1. Open your solution in Rider
2. Build the solution
3. Open the Unit Tests window (View > Tool Windows > Unit Tests)
4. DraftSpec specs appear with their context hierarchy
5. Double-click to navigate to source

## Project Structure

Recommended structure for MTP-based specs:

```
MySolution/
├── src/
│   └── MyProject/
│       ├── MyProject.csproj
│       └── Services/
│           └── TodoService.cs
├── tests/
│   └── MyProject.Specs/
│       ├── MyProject.Specs.csproj
│       ├── spec_helper.csx           # Shared setup
│       └── Services/
│           └── TodoService.spec.csx
└── MySolution.sln
```

### spec_helper.csx

Create a shared helper file for common imports and setup:

```csharp
// spec_helper.csx
#r "nuget: DraftSpec, *"
#r "../bin/Debug/net10.0/MyProject.dll"

using static DraftSpec.Dsl;
using MyProject.Services;
using MyProject.Models;

// Global test utilities
public static class TestHelpers
{
    public static TodoService CreateService() => new TodoService();
}
```

Reference it from spec files:

```csharp
// TodoService.spec.csx
#load "../spec_helper.csx"

describe("TodoService", () =>
{
    TodoService service = null!;

    before(() => service = TestHelpers.CreateService());

    it("creates todos", async () =>
    {
        var todo = await service.CreateAsync("Buy milk");
        expect(todo.Title).toBe("Buy milk");
    });
});
```

## Comparison: CLI vs MTP

| Feature | CLI (`draftspec run`) | MTP (`dotnet test`) |
|---------|----------------------|---------------------|
| Execution | Standalone tool | Integrated with .NET |
| Coverage | External tools | Built-in `--coverage` |
| IDE Integration | Basic (output only) | Full Test Explorer |
| Watch Mode | `draftspec watch` | External tools |
| Parallel | Per-file | Per-test |
| Dependencies | dotnet-script | Project references |

### When to Use CLI

- Quick prototyping and exploration
- Standalone spec scripts
- Watch mode development
- Projects not using .NET SDK

### When to Use MTP

- CI/CD integration with `dotnet test`
- Code coverage collection
- IDE Test Explorer support
- Click-to-navigate debugging
- Enterprise/team environments

## Configuration

### MTP-Specific Settings

Configure MTP behavior via `.runsettings`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <TestRunParameters>
    <Parameter name="DraftSpec.Timeout" value="30000" />
    <Parameter name="DraftSpec.Parallel" value="true" />
  </TestRunParameters>
</RunSettings>
```

Use with:

```bash
dotnet test --settings my.runsettings
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DRAFTSPEC_TIMEOUT` | Spec timeout in milliseconds | 30000 |
| `DRAFTSPEC_PARALLEL` | Enable parallel execution | true |

## Troubleshooting

### Specs Not Discovered

1. Ensure files end with `.spec.csx`
2. Check that `DraftSpec.TestingPlatform` is referenced
3. Rebuild the project

### Compilation Errors in CSX

1. Check `#r` directives point to correct paths
2. Verify referenced DLLs exist in output directory
3. Check for missing `using` statements

### Coverage Not Working

1. Ensure you're using `--collect:"XPlat Code Coverage"`
2. Check that coverlet is installed
3. Verify the test project references the code under test

### IDE Not Showing Tests

1. Rebuild the solution
2. Restart the IDE
3. Check IDE test framework extensions are installed

## Migration from CLI

To migrate from CLI-based specs to MTP:

1. Create a test project with `DraftSpec.TestingPlatform` reference
2. Copy your `.spec.csx` files to the test project
3. Update `#r` paths to use project output directory
4. Remove standalone `run()` calls (MTP handles execution)
5. Run `dotnet test`

### Before (CLI)

```csharp
#r "nuget: DraftSpec, *"
using static DraftSpec.Dsl;

describe("Calculator", () => { /* ... */ });

run();  // Explicit run call
```

### After (MTP)

```csharp
#r "nuget: DraftSpec, *"
using static DraftSpec.Dsl;

describe("Calculator", () => { /* ... */ });

// No run() call needed - MTP handles execution
```
