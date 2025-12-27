# TodoApi.Specs - MTP Integration Example

This example demonstrates DraftSpec with Microsoft Testing Platform (MTP) integration.

## Features

- Run specs via `dotnet test`
- IDE Test Explorer integration (VS, VS Code, Rider)
- Click-to-navigate from test results to source code
- Code coverage collection

## Running Tests

```bash
# Build the project under test first
cd ../TodoApi
dotnet build

# Run specs
cd ../TodoApi.Specs
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Project Structure

```
TodoApi.Specs/
├── TodoApi.Specs.csproj    # Test project with MTP references
├── spec_helper.csx         # Shared setup and fixtures
├── TodoService.spec.csx    # Service specs
└── README.md               # This file
```

## Key Differences from CLI Approach

| Aspect | CLI (`dotnet script`) | MTP (`dotnet test`) |
|--------|----------------------|---------------------|
| Execution | `dotnet script file.csx` | `dotnet test` |
| `run()` call | Required | Not needed |
| IDE Integration | Basic | Full Test Explorer |
| Coverage | External tools | Built-in `--coverage` |
| Dependencies | `#r` directives | Project references |

## Spec File Structure

```csharp
// Load shared setup
#load "spec_helper.csx"
using static DraftSpec.Dsl;

describe("MyService", () =>
{
    before(() => { /* setup */ });

    it("does something", () =>
    {
        expect(result).toBe(expected);
    });
});

// Note: No run() call - MTP handles execution
```

## IDE Integration

### Visual Studio

1. Open the solution
2. Build (Ctrl+Shift+B)
3. Open Test Explorer (Test > Test Explorer)
4. Click any test to navigate to source

### VS Code

1. Install .NET Test Explorer extension
2. Open the folder
3. Tests appear in Test Explorer sidebar

### JetBrains Rider

1. Open the solution
2. Build (Ctrl+Shift+B)
3. Open Unit Tests window
4. Double-click tests to navigate
