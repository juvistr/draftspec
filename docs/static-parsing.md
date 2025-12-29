# Static Parsing

DraftSpec v0.4.0 introduced `StaticSpecParser` - a Roslyn-based analyzer that discovers spec structure from CSX files **without executing them**. This enables fast discovery, IDE integration, and CI/CD optimization.

---

## What It Does

The parser extracts spec metadata using syntax tree analysis:

```csharp
var parser = new StaticSpecParser(projectDirectory);
var result = await parser.ParseFileAsync("specs/UserService.spec.csx");

// Returns: descriptions, context paths, line numbers, spec types
// Works even when files have compilation errors
```

### Extracted Metadata

| Field | Description |
|-------|-------------|
| `Description` | The spec description string |
| `ContextPath` | Nested hierarchy (e.g., `["UserService", "CreateAsync"]`) |
| `LineNumber` | Source location for IDE navigation |
| `Type` | Regular / Focused (`fit`) / Skipped (`xit`) |
| `IsPending` | True if spec has no body |

---

## What It Can Parse

```csharp
// Simple specs
it("creates a user", () => { });

// Nested contexts
describe("UserService", () => {
    describe("CreateAsync", () => {
        it("validates email", () => { });
    });
});

// Spec types
it("regular spec", () => { });
fit("focused spec", () => { });   // Only focused specs run
xit("skipped spec", () => { });   // Explicitly disabled

// Pending specs
it("not implemented yet");        // No body = pending

// File includes
#load "../spec_helper.csx"        // Automatically inlined
```

---

## Limitations

The parser generates warnings for patterns that cannot be analyzed statically:

```csharp
// Dynamic descriptions
var name = "user";
it($"creates a {name}", () => { });  // Warning: dynamic

// Loop-generated specs
foreach (var item in items) {
    it($"handles {item}", () => { });  // Warning: cannot analyze
}

// Conditional specs
if (condition) {
    it("conditional spec", () => { });  // May be missed
}
```

**Workarounds**:
- Use string literals for descriptions
- Use `withData()` pattern instead of loops
- Use `xit()` instead of conditional specs

---

## Performance

| Metric | Static Parsing | Execution-Based |
|--------|----------------|-----------------|
| Speed | ~5-20ms/file | ~100-500ms/file |
| Works with errors | Yes | No |
| Memory | ~10MB | ~50-100MB |

**Typical**: 100 files with 1000 specs discovered in <500ms.

---

## Use Cases

### 1. Fast Discovery (`draftspec list`)

List specs without running them - see [#186](https://github.com/juvistr/draftspec/issues/186):

```bash
draftspec list .
draftspec list . --format json --output specs.json
```

### 2. Line Number Filtering

Run specific specs by file and line - see [#188](https://github.com/juvistr/draftspec/issues/188):

```bash
draftspec run specs/UserService.spec.csx:15
draftspec run specs/UserService.spec.csx:15,23,31
```

### 3. Pre-Execution Validation

Catch errors before running - see [#187](https://github.com/juvistr/draftspec/issues/187):

```bash
draftspec validate --static
```

### 4. CI Test Partitioning

Split specs across parallel workers - see [#192](https://github.com/juvistr/draftspec/issues/192):

```bash
draftspec run --partition 4 --partition-index 0
```

### 5. IDE Integration

Static parsing enables "Run this spec" buttons in IDEs by providing:
- Line numbers for navigation
- Spec IDs for targeted execution
- Discovery from broken files

---

## Implementation Guides

Detailed implementation guides for specific features:

- [List Command Guide](./implementation-guides/LIST_COMMAND_GUIDE.md) - `draftspec list` implementation
- [Line Number Filtering Guide](./implementation-guides/LINE_NUMBER_FILTERING_GUIDE.md) - `file:line` syntax

---

## Key Files

| File | Purpose |
|------|---------|
| `src/DraftSpec.TestingPlatform/StaticSpecParser.cs` | Main parser |
| `src/DraftSpec.TestingPlatform/SpecDiscoverer.cs` | Discovery orchestration |
| `src/DraftSpec.TestingPlatform/StaticSpec.cs` | Parsed spec model |

---

## Related Issues

All static parsing features are tracked in [Epic #185](https://github.com/juvistr/draftspec/issues/185).

See [ROADMAP.md](./ROADMAP.md) for the full feature roadmap.
