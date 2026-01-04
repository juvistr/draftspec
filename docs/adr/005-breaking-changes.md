# ADR-005: Breaking Changes Policy

**Status:** Accepted
**Date:** 2025-12-15
**Deciders:** DraftSpec maintainers

## Context

DraftSpec is in active development (pre-1.0). We need to:

- Iterate quickly on API design
- Respond to user feedback
- Fix design mistakes
- Add features that may require breaking changes

**Tension:**

- Users want stability to avoid constant migration
- Maintainers need freedom to improve the framework

**Industry standards:**

- Semantic Versioning (SemVer) for version numbering
- CHANGELOG for documenting changes
- Migration guides for breaking changes

## Decision

Adopt a **staged breaking changes policy** based on version:

### Pre-1.0 (current)

- Breaking changes allowed in minor versions (0.x)
- Document all breaking changes in CHANGELOG
- Provide migration guidance for significant changes
- No deprecation period required

### Post-1.0 (future)

- Breaking changes only in major versions (x.0.0)
- Deprecate before removing (at least one minor version)
- Maintain migration guides for each major version
- Consider backward compatibility shims where feasible

### What constitutes a breaking change:

1. **Public API changes** - Renamed/removed types, methods, properties
2. **Behavior changes** - Different output for same input
3. **Default changes** - Changed default values that affect existing code
4. **Dependency updates** - Minimum framework version changes

### What is NOT a breaking change:

1. **Bug fixes** - Even if code depended on buggy behavior
2. **Performance improvements** - Unless they change semantics
3. **Internal refactoring** - Changes to non-public code
4. **Additive changes** - New APIs, new optional parameters

## Consequences

### Positive

- **Freedom to iterate** - Pre-1.0 allows rapid improvement
- **Clear expectations** - Users know what to expect at each stage
- **Documented changes** - CHANGELOG provides migration path
- **Standard approach** - SemVer is widely understood

### Negative

- **Pre-1.0 instability** - Users may hesitate to adopt
- **Migration burden** - Users must update code for breaking changes
- **Documentation work** - Each breaking change needs guidance

### Neutral

- **No compatibility guarantees** - Pre-1.0 is explicitly unstable
- **Version inflation** - Major version bumps post-1.0

## Implementation Notes

**CHANGELOG format:**

```markdown
## [0.3.0] - 2025-12-15

### Breaking Changes

- `SpecRunner.Run()` removed in favor of async-only `RunAsync()`
  - Migration: Use `await runner.RunAsync()` (async-only API)

### Added

- Async spec support via `Func<Task>` overloads

### Changed

- Middleware interface now uses async pattern

### Fixed

- Hook execution order in nested contexts
```

**Migration guide structure:**

1. Summary of breaking changes
2. Step-by-step migration instructions
3. Before/after code examples
4. Common pitfalls and solutions

**Deprecation warnings (post-1.0):**

```csharp
[Obsolete("Use RunAsync instead. Will be removed in v2.0.0")]
public List<SpecResult> Run(SpecContext context) { ... }
```
