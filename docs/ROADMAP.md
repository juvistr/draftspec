# DraftSpec Roadmap

This roadmap tracks priorities for the v1.0 release and beyond, based on a comprehensive code review (December 2025).

---

## v1.0 Blockers (P0)

Must complete before v1.0 release.

### Security ✅ Complete

- [x] **Temp file race condition** - `src/DraftSpec.Cli/SpecFileRunner.cs:131-178` ✅
  - Use `FileMode.CreateNew` for atomic file creation
  - Prevents symlink attack (CWE-367 TOCTOU)
  - See [ADR-006](./adr/006-security-hardening.md)

- [x] **Path traversal bypass** - `src/DraftSpec.Cli/SpecFinder.cs:20-24` ✅
  - Normalize paths with trailing separator
  - Use case-sensitive comparison on Unix

- [x] **JSON deserialization limits** - `src/DraftSpec.Formatters.Abstractions/SpecReport.cs:19-27` ✅
  - Add `MaxDepth = 64` to `JsonSerializerOptions`
  - Add 10MB size limit with early rejection

### Test Coverage ✅ Complete

- [x] **DSL module tests** - `tests/DraftSpec.Tests/Dsl/StaticDslTests.cs` ✅
  - 24 tests covering state isolation, configuration, edge cases, async execution
  - Coverage improved from ~30% to 90%+

- [x] **HtmlFormatter tests** - `tests/DraftSpec.Tests/Formatters/HtmlFormatterTests.cs` ✅
  - 24 tests including XSS prevention validation
  - Coverage improved from 0% to 90%+

- [x] **MarkdownFormatter tests** - `tests/DraftSpec.Tests/Formatters/MarkdownFormatterTests.cs` ✅
  - 21 tests covering all markdown syntax and formatting
  - Coverage improved from 0% to 90%+

---

## High Value (P1)

High-impact improvements after v1.0 blockers.

### Performance

- [ ] **Build parallelization** - `src/DraftSpec.Cli/SpecFileRunner.cs:58-67`
  - Parallel project resolution, 50-70% faster builds

- [ ] **Reporter batching** - `src/DraftSpec/SpecRunner.cs:205-209`
  - Batch reporter callbacks, 30-50% faster with multiple reporters

- [ ] **Watch mode debounce** - `src/DraftSpec.Cli/FileWatcher.cs:50-73`
  - Debounce rapid file changes, 60-80% allocation reduction

- [ ] **Incremental builds** - `src/DraftSpec.Cli/SpecFileRunner.cs`
  - Skip unchanged files in watch mode, 80-95% faster rebuilds

### Security

- [ ] **FileReporter path validation** - `src/DraftSpec/Plugins/Reporters/FileReporter.cs:27-46`
  - Validate output paths are within allowed directories

- [ ] **Safe error messages** - Multiple files
  - Sanitize stack traces and paths in user-facing errors

### Testing

- [ ] **CLI integration tests** - Create `tests/DraftSpec.Tests/Cli/CliIntegrationTests.cs`
  - Current coverage ~40%

---

## Architecture (P2)

Structural improvements for maintainability.

- [ ] **Remove Core→Formatters.Console dependency** - `src/DraftSpec/DraftSpec.csproj`
  - Layering violation: core should not depend on specific formatter

- [ ] **AsyncLocal state management** - `src/DraftSpec/Dsl.cs:13-26`
  - Current approach limits parallelism within a single spec file

- [ ] **Null checks in Expectation** - `src/DraftSpec/Expectations/Expectation.cs:89-98`
  - Add guards for comparison methods

- [ ] **StringBuilder capacity hints** - `src/DraftSpec.Formatters.Html/HtmlFormatter.cs`, `MarkdownFormatter.cs`
  - Pre-allocate based on expected output size

- [ ] **Interface extraction** - `SpecFileRunner`, `SpecFinder`
  - Enable testing and alternative implementations

---

## Future (P3)

Nice-to-have enhancements.

- [ ] **Result streaming** - Reduce memory for large test suites
- [ ] **DI container** - Replace ad-hoc service registry
- [ ] **Plugin discovery** - Auto-load plugins from assemblies
- [ ] **Performance benchmarks** - BenchmarkDotNet suite for regression testing

---

## Current Status

| Area | Grade | Notes |
|------|-------|-------|
| Code Quality | A- | Modern C#, good docs |
| Security | A | All P0 security issues resolved ✅ |
| Architecture | B+ | Solid foundations |
| Performance | B | Optimization opportunities |
| Test Coverage | ~85% | P0 test coverage complete ✅ |

**Overall:** All P0 items complete. Ready for production CI/CD use.

---

*Last updated: December 2025*
