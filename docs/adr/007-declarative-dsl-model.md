# ADR-007: Declarative DSL Model

**Status:** Accepted
**Date:** 2025-12-29
**Deciders:** Core team

## Context

DraftSpec originally followed a "self-executing script" model where spec files called `run()` at the end to execute themselves:

```csharp
#r "nuget: DraftSpec"
using static DraftSpec.Dsl;

describe("Calculator", () => {
    it("adds", () => expect(1 + 1).toBe(2));
});

run();  // Script executes itself
```

Scripts could also configure execution behavior inline:

```csharp
configure(runner => runner
    .WithTimeout(5000)
    .WithRetry(2)
    .WithTagFilter("fast")
);

describe("Tests", () => { /* ... */ });

run();
```

**Problems with this approach:**

1. **Dual execution models** - CLI needed to either:
   - Shell out to `dotnet script` (slow, ~2s startup per file)
   - Use in-process execution (but then `run()` would execute twice)

2. **Configuration duplication** - Same settings existed in:
   - `configure()` calls in scripts
   - CLI flags (`--parallel`, `--tags`)
   - Config file (`draftspec.json`)

3. **MTP incompatibility** - Microsoft Testing Platform discovers and runs tests itself. Scripts with `run()` would execute during discovery, causing side effects.

4. **Exit code ownership** - `run()` set `Environment.ExitCode`, but that's the CLI's responsibility for proper exit handling.

5. **Complexity** - The DSL mixed two concerns: spec definition and execution orchestration.

## Decision

Adopt a **declarative DSL model** where scripts only define specs. The framework controls execution.

### Scripts are purely declarative

```csharp
#r "nuget: DraftSpec"
using static DraftSpec.Dsl;

describe("Calculator", () => {
    it("adds", () => expect(1 + 1).toBe(2));
});
// No run() - framework handles execution
```

### Framework controls execution

| Aspect | Owner | Mechanism |
|--------|-------|-----------|
| Running specs | CLI / MTP | In-process `SpecRunner` |
| Tag filtering | CLI / MTP | `--tags` flag, `.runsettings` |
| Timeout | CLI / MTP | `draftspec.json`, environment |
| Parallelism | CLI / MTP | `--parallel` flag |
| Output format | CLI | `--format` flag |
| Exit codes | CLI | Based on results |

### Removed from DSL

- `run()` - Execution is framework responsibility
- `configure()` - Configuration via CLI flags and `draftspec.json`

### Kept in DSL

- `describe()`, `context()` - Group specs
- `it()`, `fit()`, `xit()` - Define specs
- `before()`, `after()`, `beforeAll()`, `afterAll()` - Hooks
- `expect()` - Assertions
- `tag()`, `tags()` - Categorize specs
- `withData()` - Table-driven tests

## Consequences

### Positive

1. **Single execution path** - CLI and MTP use identical in-process execution
2. **Clear separation** - DSL = what to test, Framework = how to run
3. **Simpler scripts** - No boilerplate, just specs
4. **Portable specs** - Same file works with CLI, MTP, and future integrations
5. **Faster CLI** - In-process execution eliminates `dotnet script` startup overhead

### Negative

1. **Breaking change** - Existing scripts with `run()` need updating
2. **Less script autonomy** - Can't run specs with just `dotnet script file.csx`

### Mitigations

- **Pre-1.0** - We're in prerelease, breaking changes are expected (see ADR-005)
- **Clear migration** - Remove `run()` and `configure()` calls; use CLI flags instead
- **Documentation** - Updated all docs to reflect new model

## Alternatives Considered

### 1. Keep `run()` but make it optional

Scripts could call `run()` for standalone execution, but CLI would skip it.

**Rejected:** Creates confusion about when `run()` executes. Two mental models.

### 2. Preprocess scripts to remove `run()`

CLI could strip `run()` calls before execution.

**Rejected:** Fragile regex-based approach. Doesn't solve `configure()` problem.

### 3. Make `run()` detect execution context

`run()` could check if it's being called by CLI/MTP and no-op.

**Rejected:** Hidden behavior is confusing. Doesn't address configuration duplication.

## References

- [ADR-005: Breaking Changes Policy](005-breaking-changes.md)
- [CLI Reference](../cli.md)
- [Configuration Reference](../configuration.md)
