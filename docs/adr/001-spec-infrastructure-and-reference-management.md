# ADR-001: Spec Infrastructure and Reference Management

**Status:** Proposed
**Date:** 2025-12-14
**Deciders:** TBD

## Context

DraftSpec specs currently require explicit `#r` directives to reference assemblies:

```csharp
#r "../../src/DraftSpec/bin/Debug/net10.0/DraftSpec.dll"
#r "bin/Debug/net10.0/Calculator.dll"

using static DraftSpec.Dsl;

describe("Calculator", () => {
    it("adds", () => expect(Calculator.Add(1, 2)).toBe(3));
});
```

**Problems with this approach:**

1. **Fragile paths** - Hardcoded paths break when configuration (Debug/Release) or target framework (net10.0/net11.0) changes
2. **Scattered references** - Every spec file repeats the same boilerplate
3. **No IDE support** - Without proper configuration, OmniSharp can't provide IntelliSense
4. **Onboarding friction** - New specs require knowing the correct path incantations

We considered several alternatives:

- CLI preprocessing to inject references (custom, non-standard)
- Class-based specs with attributes (abandons the lightweight CSX approach)
- Source generators (complex, requires compilation)

## Decision

Use **standard CSX conventions** with CLI tooling to reduce friction:

### 1. Centralise references in `spec_helper.csx`

A single file per project handles all reference management:

```csharp
// spec_helper.csx
#r "nuget: DraftSpec"
#r "bin/Debug/net10.0/Calculator.dll"

using static DraftSpec.Dsl;

// Shared fixtures and utilities can go here
```

Individual specs become clean:

```csharp
// Calculator.spec.csx
#load "spec_helper.csx"

describe("Calculator", () => {
    it("adds", () => expect(Calculator.Add(1, 2)).toBe(3));
});
```

### 2. CLI commands to initialise infrastructure

```bash
draftspec init    # Create spec_helper.csx with resolved references
draftspec new Foo # Scaffold a new spec file with #load
```

**`draftspec init`** will:

1. Detect `.csproj` in current directory
2. Query MSBuild for `TargetPath` to get the correct DLL path
3. Generate `spec_helper.csx` with resolved references
4. Generate `omnisharp.json` for IDE tooling support
5. Optionally scaffold a starter spec

**`draftspec new <Name>`** will:

1. Create `<Name>.spec.csx` with `#load "spec_helper.csx"`
2. Scaffold a basic `describe`/`it` structure

### 3. IDE support via omnisharp.json

The CLI will generate an `omnisharp.json` to enable OmniSharp features:

```json
{
  "script": {
    "enableScriptNuGetReferences": true,
    "defaultTargetFramework": "net10.0"
  }
}
```

This enables:

- IntelliSense for DraftSpec DSL and loaded fixtures
- Go to Definition across `#load` boundaries
- Error squiggles for type mismatches

## Consequences

### Positive

- **Standard CSX** - No custom preprocessing or magic; `#load` and `#r` are native dotnet-script features
- **Single source of truth** - Reference paths live in one file per project
- **IDE support** - OmniSharp can provide IntelliSense with proper configuration
- **Low friction** - CLI handles the setup; users just write specs
- **Extensible** - `spec_helper.csx` is a natural place for shared fixtures, factories, utilities

### Negative

- **DLL path still fragile** - The path in `spec_helper.csx` still references a specific configuration/framework, though it's now in one place
- **One extra line per spec** - Each spec needs `#load "spec_helper.csx"` (could be auto-injected by CLI if desired)
- **OmniSharp reliability** - CSX tooling support is historically less robust than regular .cs files

### Neutral

- **NuGet for DraftSpec** - Referencing DraftSpec via NuGet (`#r "nuget: DraftSpec"`) is idiomatic but requires publishing (local feed is fine for development)
- **Convention over configuration** - The approach assumes specs live alongside their target project

## Alternatives Considered

### A. CLI preprocessing (inject #r directives)

The CLI would read spec files, inject correct `#r` directives based on MSBuild queries, and write temp files for execution.

**Rejected because:** Non-standard; breaks IDE tooling; adds complexity to understand what's happening.

### B. Class-based specs with attributes

```csharp
[Spec]
public class CalculatorSpec
{
    [It("adds")]
    public void Adds() => expect(Calculator.Add(1, 2)).toBe(3);
}
```

**Rejected because:** Loses the lightweight scripting appeal; becomes "just another test framework"; requires compilation.

### C. Source generators for spec skeletons

Generate spec stubs from interfaces with `[GenerateSpecs]` attributes.

**Deferred:** Interesting for tracking pending specs (like typed holes), but doesn't solve the reference problem and adds significant complexity.

### D. Custom #draftspec-ref directive

```csharp
#draftspec-ref "../Calculator/Calculator.csproj"
```

**Rejected because:** Non-standard; requires custom parsing; doesn't compose with IDE tooling.

## Implementation Plan

1. Add `init` command to CLI

   - Detect `.csproj` and query MSBuild for output path
   - Generate `spec_helper.csx` template
   - Generate `omnisharp.json`

2. Add `new` command to CLI

   - Accept spec name argument
   - Generate spec file with `#load` and scaffold

3. Publish DraftSpec as NuGet package

   - Local feed initially
   - Public later when stable

4. Update Calculator example
   - Add `spec_helper.csx`
   - Simplify `Calculator.spec.csx` to use `#load`

## References

- [dotnet-script documentation](https://github.com/dotnet-script/dotnet-script)
- [OmniSharp CSX support](https://github.com/OmniSharp/omnisharp-roslyn)
- [MADR template](https://adr.github.io/madr/)
