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

- [x] **Build parallelization** - `src/DraftSpec.Cli/SpecFileRunner.cs:64-79` ✅
  - Build projects in different directories in parallel when --parallel flag is used
  - Projects within same directory still built sequentially (may have interdependencies)

- [x] **Reporter batching** - `src/DraftSpec/SpecRunner.cs` ✅
  - `OnSpecsBatchCompletedAsync` method for batch notification
  - Parallel notification to multiple reporters via `Task.WhenAll`
  - 30-50% faster with multiple reporters

- [x] **Watch mode debounce** - `src/DraftSpec.Cli/FileWatcher.cs` ✅
  - Replaced Task.Run + CancellationTokenSource with single reusable Timer
  - No allocations on rapid file changes (60-80% reduction)

- [x] **Incremental builds** - `src/DraftSpec.Cli/SpecFileRunner.cs` ✅
  - Track source file modification times per directory
  - Skip rebuilds when no .cs or .csproj changes detected
  - OnBuildSkipped event for UI feedback in watch mode

### Security

- [x] **FileReporter path validation** - `src/DraftSpec/Plugins/Reporters/FileReporter.cs` ✅
  - Validate output paths are within allowed directories
  - Uses same pattern as SpecFinder (trailing separator, platform-aware)
  - 14 tests in `tests/DraftSpec.Tests/Reporters/FileReporterTests.cs`

- [x] **Safe error messages** - `src/DraftSpec.Cli/Program.cs`, `RunCommand.cs` ✅
  - Generic catch-all prevents stack trace leakage
  - SecurityException handling added
  - Platform-aware path validation (case-sensitive on Unix)
  - Error messages use user-provided paths, not internal paths

### Testing

- [x] **CLI integration tests** - `tests/DraftSpec.Tests/Cli/CliIntegrationTests.cs` ✅
  - 30 tests covering InitCommand, NewCommand, GetFormatter, SpecFinder, CliOptions
  - Coverage improved from ~40% to ~70%

---

## Architecture (P2) ✅ Complete

Structural improvements for maintainability.

- [x] **Remove Core→Formatters.Console dependency** - `src/DraftSpec/DraftSpec.csproj` ✅
  - Removed project reference from Core to Formatters.Console
  - Added `IConsoleFormatter` property to `DraftSpecConfiguration`
  - Plain text fallback in `SpecExecutor` when no formatter configured

- [x] **Null checks in Expectation** - `src/DraftSpec/Expectations/Expectation.cs:89-154` ✅
  - Added guards for comparison methods: toBeGreaterThan, toBeLessThan, toBeAtLeast, toBeAtMost, toBeInRange
  - Proper error messages with Expression fallback

- [x] **StringBuilder optimization** - `src/DraftSpec.Formatters.Html/HtmlFormatter.cs`, `MarkdownFormatter.cs` ✅
  - Replaced List<string> + string.Join with direct StringBuilder appends
  - Eliminates intermediate allocations in summary output

- [x] **Interface extraction** - `ISpecFileRunner`, `ISpecFinder` ✅
  - `src/DraftSpec.Cli/ISpecFinder.cs` - FindSpecs interface
  - `src/DraftSpec.Cli/ISpecFileRunner.cs` - Full runner interface with events
  - Enables dependency injection and testing

- [x] **AsyncLocal state management** - `src/DraftSpec/Spec.cs` ✅
  - Static `Dsl` keeps AsyncLocal (necessary for CSX scripts)
  - `Spec` class refactored to instance fields for clean isolation
  - No API changes - internal improvement only

---

## Future (P3) ✅ Complete

Nice-to-have enhancements.

- [x] **Result streaming** - Reduce memory for large test suites ✅
  - `StreamingStats` class for progressive statistic tracking
  - `StreamingConsoleReporter` outputs dots/symbols as specs complete
  - Thread-safe tracking via `Interlocked` operations
- [x] **DI container** - Replace ad-hoc service registry ✅
  - `Microsoft.Extensions.DependencyInjection` integration in CLI
  - `IFormatterRegistry` for name-based formatter lookup
  - `ServiceCollectionExtensions.AddDraftSpec()` for easy setup
- [x] **Plugin discovery** - Auto-load plugins from assemblies ✅
  - `PluginLoader` scans `DraftSpec.*.dll` files in plugins directory
  - `[DraftSpecPlugin("name")]` attribute for marking discoverable plugins
  - Isolated `AssemblyLoadContext` for plugin loading
- [x] **Performance benchmarks** - BenchmarkDotNet suite for regression testing ✅
  - `benchmarks/DraftSpec.Benchmarks/` project with 26 benchmarks
  - SpecRunner, ReportBuilder, Formatter, and Expectation benchmarks
  - Memory diagnostics enabled via `[MemoryDiagnoser]`
  - Baseline captured in `docs/BENCHMARK_BASELINE.md`

---

## Current Status

| Area | Grade | Notes |
|------|-------|-------|
| Code Quality | A | Modern C#, good docs, clean layering |
| Security | A | All P0 + P1 security issues resolved ✅ |
| Architecture | A | P2 + P3 complete, DI container + plugin discovery ✅ |
| Performance | A | Zero-allocation assertions, benchmarks, streaming ✅ |
| Test Coverage | ~90% | 515 tests, comprehensive coverage ✅ |

**Overall:** P0, P1, P2, and P3 complete. Production ready with extensible plugin architecture.

---

*Last updated: December 2025*
