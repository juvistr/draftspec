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

**Status:** Complete (commit e7d2ba6) - A1/A2 deferred to Phase 3

### Architecture

- [ ] **A1: Extract IReporter interface** *(deferred to Phase 3)*
  - Files: Create `src/DraftSpec/Reporting/IReporter.cs`, `ConsoleReporter.cs`, `JsonReporter.cs`
  - Action: Extract reporting from Dsl.cs
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#1-reporting-layer-currently-embedded-in-dslcs)

- [ ] **A2: Split Dsl.cs (God object)** *(deferred to Phase 3)*
  - File: `src/DraftSpec/Dsl.cs` (460 lines)
  - Action: Extract context management, execution, reporting
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

## Phase 3: Extensibility

**Goal:** Enable community extensions and advanced use cases

### Architecture

- [ ] **A5: Add execution pipeline with middleware**
  - Files: Create `src/DraftSpec/Execution/IExecutionPipeline.cs`, `ISpecMiddleware.cs`
  - Action: Enable retry, timeout, parallel execution
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#2-execution-pipeline-no-extension-points)

- [ ] **A6: Add spec filtering API**
  - File: `src/DraftSpec/SpecRunner.cs`
  - Action: Filter by name pattern, tags, custom predicate
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#4-no-spec-filtering-api)

- [ ] **A7: Add async spec support**
  - File: `src/DraftSpec/Dsl.cs`
  - Action: Add Func<Task> overloads for it()
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#10-missing-async-support)

- [ ] **A8: Plugin registration system**
  - Files: Create `src/DraftSpec/DraftSpecConfiguration.cs`
  - Action: Enable AddReporter, AddMiddleware configuration
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#phase-2-extensibility-layer)

### Code Quality

- [ ] **C3: Eliminate duplicate Format methods**
  - Files: `src/DraftSpec/Expectations/*.cs`
  - Action: Extract to shared ExpectationHelpers
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#4-duplicate-format-methods)

- [ ] **C4: Extract magic strings to constants**
  - File: `src/DraftSpec.Cli/Commands/RunCommand.cs`
  - Action: Create OutputFormats constants
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#5-magic-strings-for-output-formats)

### Security

- [ ] **S4: Sanitize CustomCss in HTML formatter**
  - File: `src/DraftSpec.Formatters.Html/HtmlFormatter.cs`
  - Action: Escape or validate CSS content
  - See: [SECURITY.md](./SECURITY.md#m-3-xss-in-html-formatter)

- [ ] **S5: Add SECURITY.md**
  - File: Create `SECURITY.md`
  - Action: Document security reporting, safe usage
  - See: [SECURITY.md](./SECURITY.md#informational)

### Testing

- [ ] **T5: Add edge case tests** (~20 tests)
  - Files: Create `tests/DraftSpec.Tests/EdgeCases/*.cs`
  - Action: Hook exceptions, deep nesting, large suites
  - See: [TEST_COVERAGE.md](./TEST_COVERAGE.md#5-edge-cases-5-coverage)

- [ ] **T6: Add integration tests** (~10 tests)
  - Files: Create `tests/DraftSpec.Tests/Integration/*.cs`
  - Action: End-to-end scenarios
  - See: [TEST_COVERAGE.md](./TEST_COVERAGE.md#6-integration-testing-0-coverage)

- [ ] **T7: Set up coverage in CI**
  - File: `.github/workflows/ci.yml` (or equivalent)
  - Action: Add coverage reporting, 85% threshold
  - See: [TEST_COVERAGE.md](./TEST_COVERAGE.md#ci-integration)

## Phase 4: Polish

**Goal:** Documentation, performance monitoring, advanced features

### Documentation

- [ ] **D1: Add architecture documentation**
  - File: Create `docs/ARCHITECTURE.md`
  - Action: Document component diagrams, extension points
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#documentation-gaps)

- [ ] **D2: Add XML documentation**
  - Files: All public APIs
  - Action: Add param, returns, exception docs
  - See: [CODE_QUALITY.md](./CODE_QUALITY.md#13-missing-xml-documentation)

- [ ] **D3: Create ADRs for design decisions**
  - Files: `docs/adr/002-*.md`, etc.
  - Action: Document reporter pattern, pipeline design
  - See: [ARCHITECTURE.md](./ARCHITECTURE.md#adrs-needed)

### Performance

- [ ] **P4: Add performance instrumentation**
  - File: `src/DraftSpec/SpecRunner.cs`
  - Action: Add optional metrics output
  - See: [PERFORMANCE.md](./PERFORMANCE.md#benchmarking-targets)

- [ ] **P5: Add performance regression tests**
  - File: Create `tests/DraftSpec.Tests/Performance/*.cs`
  - Action: Benchmark large suites
  - See: [PERFORMANCE.md](./PERFORMANCE.md#suggested-benchmarks)

- [ ] **P6: Parallel execution engine**
  - Files: `src/DraftSpec/Execution/ParallelRunner.cs`
  - Action: Enable parallel spec execution
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
| Security | 3 | 0 | 2 | 0 | 5 | 3 ✅ |
| Architecture | 0 | 4 | 4 | 0 | 8 | 2 ✅ |
| Code Quality | 2 | 0 | 2 | 0 | 4 | 2 ✅ |
| Performance | 0 | 3 | 0 | 3 | 6 | 3 ✅ |
| Testing | 2 | 2 | 3 | 0 | 7 | 4 ✅ |
| Documentation | 0 | 0 | 0 | 3 | 3 | 0 |
| Features | 0 | 0 | 0 | 3 | 3 | 0 |
| **Total** | **7** | **9** | **11** | **9** | **36** | **14** |

## Progress

- **Phase 1:** ✅ Complete (7/7 items)
- **Phase 2:** ✅ Complete (7/9 items, 2 deferred)
- **Phase 3:** Not started
- **Phase 4:** Not started

## Quick Reference

### Files Most Needing Work

1. ~~`src/DraftSpec.Cli/SpecFileRunner.cs` - Security fixes~~ ✅
2. ~~`src/DraftSpec.Cli/SpecFinder.cs` - Path validation~~ ✅
3. ~~`src/DraftSpec/SpecRunner.cs` - Performance + interface~~ ✅
4. `src/DraftSpec/Dsl.cs` - Split into smaller classes (A1/A2)
5. `tests/DraftSpec.Tests/*` - Continue adding tests

### Metrics to Track

| Metric | Initial | After Phase 1 | After Phase 2 | Target |
|--------|---------|---------------|---------------|--------|
| Test Count | 9 | 111 | 155 | 200+ |
| Security Issues (High) | 3 | 0 ✅ | 0 ✅ | 0 |
| Extension Points | 2 | 2 | 4 | 8+ |
| Perf Optimizations | 0 | 0 | 3 ✅ | 6 |
