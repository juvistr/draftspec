# Action Plan

Prioritized work items from the comprehensive review, organized into phases.

## Phase 1: Critical Fixes ✅

**Goal:** Address security vulnerabilities and critical bugs

**Status:** Complete (commit f7aebfd)

### Security (Must Fix)

- [x] **S1: Fix path traversal vulnerability**
  - File: `src/DraftSpec.Cli/SpecFinder.cs`
  - Action: Validate paths are within project directory
  - See: [SECURITY.md](./SECURITY.md#h-2-path-traversal-in-spec-file-discovery)

- [x] **S2: Fix command injection risk**
  - File: `src/DraftSpec.Cli/ProcessHelper.cs`
  - Action: Use ArgumentList instead of Arguments string
  - See: [SECURITY.md](./SECURITY.md#h-1-command-injection-via-unvalidated-file-paths)

- [x] **S3: Secure temp file handling**
  - File: `src/DraftSpec.Cli/SpecFileRunner.cs`
  - Action: Use cryptographically random temp file names
  - See: [SECURITY.md](./SECURITY.md#h-3-insecure-temporary-file-handling)

### Testing (Critical)

- [x] **T1: Add expectation API tests** (~83 tests)
  - Files: `tests/DraftSpec.Tests/Expectations/*.cs`
  - Action: Test toBe, toContain, toThrow, etc.
  - See: [TEST_COVERAGE.md](./TEST_COVERAGE.md#1-expectation-api-0-coverage)

- [x] **T2: Add basic runner tests** (~19 tests)
  - File: `tests/DraftSpec.Tests/Runner/SpecRunnerTests.cs`
  - Action: Test execution, exception capture, duration
  - See: [TEST_COVERAGE.md](./TEST_COVERAGE.md#3-core-infrastructure-0-coverage)

### Code Quality (High Priority)

- [x] **C1: Add validation to SpecContext**
  - File: `src/DraftSpec/SpecContext.cs`
  - Action: Validate description is not null/empty
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#3-missing-validation-in-speccontext)

- [x] **C2: Fix string replacement fragility**
  - File: `src/DraftSpec.Cli/SpecFileRunner.cs`
  - Action: Use regex with word boundaries
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#2-string-replacement-fragility)

## Phase 2: Architecture Refactoring ✅

**Goal:** Improve extensibility and separation of concerns

**Status:** Complete (commit e7d2ba6, 77c361c)

### Architecture

- [x] **A1: Unify output systems with IFormatter** *(commit 77c361c)*
  - Files: `IConsoleFormatter.cs`, `ConsoleFormatter.cs`, `SpecReportBuilder.cs`
  - Action: Unified internal Dsl.cs output with IFormatter architecture
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#1-reporting-layer-currently-embedded-in-dslcs)

- [x] **A2: Split Dsl.cs (God object)**
  - Files: `Dsl.cs`, `Dsl.Context.cs`, `Dsl.Specs.cs`, `Dsl.Hooks.cs`, `Dsl.Expect.cs`, `Dsl.Run.cs`
  - Files: `Internal/ContextBuilder.cs`, `Internal/SpecExecutor.cs`
  - Action: Split into partial classes + internal helpers for shared logic
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#1-god-object-dslcs-460-lines)

- [x] **A3: Add ISpecRunner interface**
  - File: `src/DraftSpec/ISpecRunner.cs`
  - Action: Extract interface for testability
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#recommended-architecture-evolution)

- [x] **A4: Make collections immutable**
  - File: `src/DraftSpec/SpecContext.cs`
  - Action: Return IReadOnlyList instead of List
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#7-mutable-public-collections)

### Performance

- [x] **P1: Fix O(n²) JSON algorithm**
  - File: `src/DraftSpec/Dsl.cs`
  - Action: Use Dictionary lookup instead of FirstOrDefault
  - See: [PERFORMANCE.md](./PERFORMANCE.md#1-on²-json-tree-building-algorithm)

- [x] **P2: Cache hook chains**
  - File: `src/DraftSpec/SpecContext.cs`
  - Action: Cache hook chains lazily in SpecContext
  - See: [PERFORMANCE.md](./PERFORMANCE.md#2-hook-chain-reconstruction-per-spec)

- [x] **P3: Add early exit to focus detection**
  - File: `src/DraftSpec/SpecRunner.cs`
  - Action: Return immediately when focused spec found
  - See: [PERFORMANCE.md](./PERFORMANCE.md#3-focus-detection-full-tree-scan)

### Testing

- [x] **T3: Add DSL tests** (~27 tests)
  - Files: `tests/DraftSpec.Tests/Dsl/DslTests.cs`
  - Action: Test describe/it/context/hooks
  - See: [TEST_COVERAGE.md](./TEST_COVERAGE.md#2-dsl-20-coverage)

- [x] **T4: Add output tests** (~17 tests)
  - Files: `tests/DraftSpec.Tests/Output/OutputTests.cs`
  - Action: Test console formatting, JSON structure
  - See: [TEST_COVERAGE.md](./TEST_COVERAGE.md#4-output-0-coverage)

## Phase 3: Extensibility ✅

**Goal:** Enable community extensions and advanced use cases

**Status:** Complete

### Architecture

- [x] **A5: Add execution pipeline with middleware** *(commit b9f1333)*
  - Files: `src/DraftSpec/Middleware/ISpecMiddleware.cs`, `SpecExecutionContext.cs`, `RetryMiddleware.cs`, `TimeoutMiddleware.cs`
  - Files: `src/DraftSpec/SpecRunnerBuilder.cs`, `Dsl.Configuration.cs`
  - Action: Added middleware pipeline with retry and timeout support
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#2-execution-pipeline-no-extension-points)

- [x] **A6: Add spec filtering API** *(commit pending)*
  - Files: `src/DraftSpec/Middleware/FilterMiddleware.cs`, `SpecRunnerBuilder.cs`, `Dsl.Tags.cs`
  - Action: Filter by name pattern (regex), tags, and custom predicates via middleware
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#4-no-spec-filtering-api)

- [x] **A7: Add async spec support** *(commit pending)*
  - Files: `src/DraftSpec/Dsl.Specs.cs`, `Dsl.Hooks.cs`, `SpecRunner.cs`, `ISpecMiddleware.cs`
  - Action: Async-first pipeline with `Func<Task>` overloads for specs and hooks
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#10-missing-async-support)

- [x] **A8: Plugin registration system** *(commit pending)*
  - Files: `src/DraftSpec/Plugins/*.cs`, `src/DraftSpec/Configuration/*.cs`
  - Files: `src/DraftSpec/Plugins/Reporters/FileReporter.cs`, `ConsoleReporter.cs`
  - Files: `src/DraftSpec.Formatters.Abstractions/JsonFormatter.cs`
  - Action: Full plugin system with IPlugin, IReporter, formatter/reporter registries
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#phase-2-extensibility-layer)

### Code Quality

- [x] **C3: Eliminate duplicate Format methods**
  - Files: `src/DraftSpec/Expectations/*.cs`
  - Action: Extract to shared ExpectationHelpers
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#4-duplicate-format-methods)

- [x] **C4: Extract magic strings to constants**
  - File: `src/DraftSpec.Cli/Commands/RunCommand.cs`
  - Action: Create OutputFormats constants
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#5-magic-strings-for-output-formats)

### Security

- [x] **S4: Sanitize CustomCss in HTML formatter**
  - File: `src/DraftSpec.Formatters.Html/HtmlFormatter.cs`
  - Action: Escape or validate CSS content
  - See: [SECURITY.md](./SECURITY.md#m-3-xss-in-html-formatter)

- [x] **S5: Add SECURITY.md**
  - File: Create `SECURITY.md`
  - Action: Document security reporting, safe usage
  - See: [SECURITY.md](./SECURITY.md#informational)

### Testing

- [x] **T5: Add edge case tests** (~20 tests)
  - Files: Create `tests/DraftSpec.Tests/EdgeCases/*.cs`
  - Action: Hook exceptions, deep nesting, large suites
  - See: [TEST_COVERAGE.md](./TEST_COVERAGE.md#5-edge-cases-5-coverage)

- [x] **T6: Add integration tests** (~10 tests)
  - Files: Create `tests/DraftSpec.Tests/Integration/*.cs`
  - Action: End-to-end scenarios
  - See: [TEST_COVERAGE.md](./TEST_COVERAGE.md#6-integration-testing-0-coverage)

- [x] **T7: Set up coverage in CI**
  - File: `.github/workflows/ci.yml` (or equivalent)
  - Action: Add coverage reporting, 85% threshold
  - See: [TEST_COVERAGE.md](./TEST_COVERAGE.md#ci-integration)

## Phase 4: Polish ✅

**Goal:** Documentation, performance monitoring, advanced features

**Status:** Complete

### Documentation

- [x] **D1: Add architecture documentation**
  - File: Created `docs/ARCHITECTURE.md`
  - Action: Component diagrams, extension points, hook order
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#documentation-gaps)

- [x] **D2: Add XML documentation**
  - Files: All 32 public APIs documented
  - Action: Added param, returns, exception docs
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#13-missing-xml-documentation)

- [x] **D3: Create ADRs for design decisions**
  - Files: `docs/adr/002-reporter-system.md`, `003-middleware-pipeline.md`, `004-plugin-architecture.md`, `005-breaking-changes.md`
  - Action: Documented reporter pattern, pipeline design, plugin system, versioning policy
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#adrs-needed)

### Performance

- [x] **P4: Add performance instrumentation**
  - Files: `src/DraftSpec/SpecResult.cs`, `src/DraftSpec/SpecRunner.cs`
  - Action: Added BeforeEachDuration, AfterEachDuration, TotalDuration properties; instrumented hook timing
  - See: [PERFORMANCE.md](./PERFORMANCE.md#benchmarking-targets)

- [x] **P5: Add performance regression tests**
  - File: Created `tests/DraftSpec.Tests/Performance/PerformanceTests.cs`
  - Action: Benchmarks for large suites, deep nesting, hook chains, focus detection, middleware overhead
  - See: [PERFORMANCE.md](./PERFORMANCE.md#suggested-benchmarks)

- [x] **P6: Parallel execution engine**
  - Files: `src/DraftSpec/SpecRunner.cs`, `src/DraftSpec/SpecRunnerBuilder.cs`, `src/DraftSpec/Middleware/SpecExecutionContext.cs`
  - Action: Added WithParallelExecution() API, Parallel.ForEachAsync, ConcurrentDictionary for thread safety
  - See: [PERFORMANCE.md](./PERFORMANCE.md#concurrency-opportunities)

### Advanced Features

- [ ] **F1: Custom matcher extension API**
  - Files: Document or provide base classes
  - Action: Enable domain-specific assertions
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#3-no-extension-points-for-custom-matchers)

- [ ] **F2: Watch mode**
  - Action: Re-run on file changes
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#phase-3-advanced-features)

- [ ] **F3: Snapshot testing**
  - Action: Compare output to saved snapshots
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#phase-3-advanced-features)

## Summary by Category

| Category | Phase 1 | Phase 2 | Phase 3 | Phase 4 | Total | Done |
|----------|---------|---------|---------|---------|-------|------|
| Security | 3 | 0 | 2 | 0 | 5 | 5 ✅ |
| Architecture | 0 | 4 | 4 | 0 | 8 | 8 ✅ |
| Code Quality | 2 | 0 | 2 | 0 | 4 | 4 ✅ |
| Performance | 0 | 3 | 0 | 3 | 6 | 6 ✅ |
| Testing | 2 | 2 | 3 | 0 | 7 | 7 ✅ |
| Documentation | 0 | 0 | 0 | 3 | 3 | 3 ✅ |
| Features | 0 | 0 | 0 | 3 | 3 | 0 |
| **Total** | **7** | **9** | **11** | **9** | **36** | **33** |

## Progress

- **Phase 1:** ✅ Complete (7/7 items)
- **Phase 2:** ✅ Complete (9/9 items)
- **Phase 3:** ✅ Complete (11/11 items)
- **Phase 4:** ✅ Complete (6/6 items - Documentation + Performance)

## Quick Reference

### Files Most Needing Work

1. ~~`src/DraftSpec.Cli/SpecFileRunner.cs` - Security fixes~~ ✅
2. ~~`src/DraftSpec.Cli/SpecFinder.cs` - Path validation~~ ✅
3. ~~`src/DraftSpec/SpecRunner.cs` - Performance + interface~~ ✅
4. ~~`src/DraftSpec/Dsl.cs` - Split into smaller classes (A1/A2)~~ ✅
5. `tests/DraftSpec.Tests/*` - Continue adding tests

### Metrics to Track

| Metric | Initial | After Phase 1 | After Phase 2 | After Phase 3 | After Phase 4 | Target |
|--------|---------|---------------|---------------|---------------|---------------|--------|
| Test Count | 9 | 111 | 192 | 299 | 332 | 200+ ✅ |
| Security Issues (High) | 3 | 0 ✅ | 0 ✅ | 0 ✅ | 0 ✅ | 0 |
| Extension Points | 2 | 2 | 6 | 12 | 13 | 8+ ✅ |
| Perf Optimizations | 0 | 0 | 3 | 3 | 6 ✅ | 6 |
