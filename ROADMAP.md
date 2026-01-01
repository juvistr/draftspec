# DraftSpec Roadmap

This roadmap tracks development priorities from the initial v0.4.0 release through future milestones.

---

## Current Status: v0.7.0-alpha.1 (Prerelease)

**Released January 2026** - Test intelligence features complete, undergoing additional dogfooding before stable release.

| Area | Status | Highlights |
|------|--------|------------|
| Core DSL | Complete | `describe`/`it`/`expect` API, hooks, focus/skip/pending |
| Security | Complete | Path traversal protection, temp file hardening, JSON limits, plugin signing |
| Performance | Complete | Zero-allocation assertions, incremental builds, parallel execution |
| Architecture | Complete | DI container, plugin discovery, result streaming |
| Test Coverage | ~90% | 550+ tests across all modules |
| Static Parsing | Complete | Roslyn-based spec discovery from CSX files |
| MTP Integration | Complete | Microsoft.Testing.Platform support for IDE integration |
| Test Intelligence | Complete | Dependency graphs, result caching, flaky detection, runtime estimation |

---

## v0.5.0: CLI Foundation ✅

**Theme**: Core CLI commands leveraging static parsing for fast spec discovery.

| Issue | Feature | Status |
|-------|---------|--------|
| [#186](https://github.com/juvistr/draftspec/issues/186) | `draftspec list` - Discover and display specs | ✅ |
| [#187](https://github.com/juvistr/draftspec/issues/187) | `draftspec validate --static` - Pre-execution validation | ✅ |
| [#188](https://github.com/juvistr/draftspec/issues/188) | Line number filtering (`file:line` syntax) | ✅ |
| [#190](https://github.com/juvistr/draftspec/issues/190) | Enhanced compilation error messages | ✅ |
| [#189](https://github.com/juvistr/draftspec/issues/189) | Context-level filtering (`--context` flag) | ✅ |
| [#191](https://github.com/juvistr/draftspec/issues/191) | JSON schema for discovery output | ✅ |

---

## v0.6.0: CI/CD Integration ✅

**Theme**: Features that optimize CI pipelines and development workflows.

| Issue | Feature | Status |
|-------|---------|--------|
| [#192](https://github.com/juvistr/draftspec/issues/192) | Parallel test partitioning (`--partition`) | ✅ |
| [#193](https://github.com/juvistr/draftspec/issues/193) | Incremental watch mode (only re-run changed specs) | ✅ |
| [#194](https://github.com/juvistr/draftspec/issues/194) | Pre-flight validation for CI pipelines | ✅ |
| [#196](https://github.com/juvistr/draftspec/issues/196) | Spec count statistics before run | ✅ |
| [#195](https://github.com/juvistr/draftspec/issues/195) | GitHub Actions workflow templates | ✅ |

---

## v0.6.1: Testability Foundations ✅

**Theme**: Extract interfaces for better unit testing and parallel test execution.

| Issue | Feature | Status |
|-------|---------|--------|
| [#253](https://github.com/juvistr/draftspec/issues/253) | Extract ISpecFileProvider for testable file discovery | ✅ |
| [#254](https://github.com/juvistr/draftspec/issues/254) | Extract ISpecStateManager for state isolation | ✅ |

---

## v0.6.2: Repository Quality ✅

**Theme**: OSS best practices, automation, and contributor experience.

| Issue | Feature | Status |
|-------|---------|--------|
| [#276](https://github.com/juvistr/draftspec/issues/276) | CODEOWNERS, Dependabot, EditorConfig, global.json | ✅ |
| [#277](https://github.com/juvistr/draftspec/issues/277) | Roslyn analyzers (StyleCop + Roslynator) | ✅ |
| [#278](https://github.com/juvistr/draftspec/issues/278) | Pre-commit hooks via Husky.Net | ✅ |
| [#279](https://github.com/juvistr/draftspec/issues/279) | GitHub automation (stale bot, release drafter, all-contributors) | ✅ |
| [#280](https://github.com/juvistr/draftspec/issues/280) | Discussion templates | ✅ |
| [#281](https://github.com/juvistr/draftspec/issues/281) | OpenSSF Scorecard badge + VS Code settings | ✅ |

---

## v0.7.0: Test Intelligence ✅

**Theme**: Smart test selection and historical analysis.

| Issue | Feature | Status |
|-------|---------|--------|
| [#207](https://github.com/juvistr/draftspec/issues/207) | Spec authoring best practices guide | ✅ |
| [#198](https://github.com/juvistr/draftspec/issues/198) | Dependency graph from `#load` directives | ✅ |
| [#201](https://github.com/juvistr/draftspec/issues/201) | Result caching for watch mode | ✅ |
| [#197](https://github.com/juvistr/draftspec/issues/197) | Test impact analysis (`--affected-by`) | ✅ |
| [#199](https://github.com/juvistr/draftspec/issues/199) | Flaky test detection and quarantine | ✅ |
| [#200](https://github.com/juvistr/draftspec/issues/200) | Runtime estimation from historical data | ✅ |

---

## v0.7.1: Patch Release

**Theme**: Address review findings from v0.7.0 and improve reliability.

| Issue | Feature | Priority |
|-------|---------|----------|
| [#358](https://github.com/juvistr/draftspec/issues/358) | Fix rate limiter TOCTOU race condition | HIGH |
| [#359](https://github.com/juvistr/draftspec/issues/359) | Eliminate redundant file hashing in caches | MEDIUM |
| [#360](https://github.com/juvistr/draftspec/issues/360) | Optimize RuntimeEstimator LINQ iterations | MEDIUM |
| [#361](https://github.com/juvistr/draftspec/issues/361) | Avoid double preprocessing on cache miss | MEDIUM |
| [#362](https://github.com/juvistr/draftspec/issues/362) | Add logging to silent exception handlers | MEDIUM |
| [#363](https://github.com/juvistr/draftspec/issues/363) | Enhance plugin signing with cert chain validation | MEDIUM |
| [#365](https://github.com/juvistr/draftspec/issues/365) | Extract common base class for disk caches | LOW |
| [#310](https://github.com/juvistr/draftspec/issues/310) | Set up mutation testing with Stryker.NET | LOW |

---

## v0.8.0: Developer Experience

**Theme**: Advanced DX features and ecosystem expansion.

| Issue | Feature | Priority |
|-------|---------|----------|
| [#206](https://github.com/juvistr/draftspec/issues/206) | Enhanced MTP Test Explorer integration | MEDIUM |
| [#202](https://github.com/juvistr/draftspec/issues/202) | Living documentation generation | HIGH |
| [#203](https://github.com/juvistr/draftspec/issues/203) | Spec coverage mapping | MEDIUM |
| [#204](https://github.com/juvistr/draftspec/issues/204) | Interactive spec selection (`--interactive`) | MEDIUM |
| [#205](https://github.com/juvistr/draftspec/issues/205) | Rider/VS Code CodeLens plugins | LOW |

---

## v0.9.x: Framework Expansion

Split into focused sub-milestones for parallel development.

### v0.9.0-DSL: Foundation Layer

| Issue | Feature |
|-------|---------|
| [#217](https://github.com/juvistr/draftspec/issues/217) | Multiple hooks per context |
| [#218](https://github.com/juvistr/draftspec/issues/218) | `let()` lazy fixtures |
| [#219](https://github.com/juvistr/draftspec/issues/219) | Around hooks |
| [#225](https://github.com/juvistr/draftspec/issues/225) | Shared examples |

### v0.9.1-Assertions: Feature Layer

| Issue | Feature |
|-------|---------|
| [#215](https://github.com/juvistr/draftspec/issues/215) | `.because()` contextual messages |
| [#213](https://github.com/juvistr/draftspec/issues/213) | `toBeOneOf()` / `toSatisfy()` matchers |
| [#214](https://github.com/juvistr/draftspec/issues/214) | DateTime-specific matchers |
| [#216](https://github.com/juvistr/draftspec/issues/216) | Collection matchers |

### v0.9.2-Output: Formatters & CLI

| Issue | Feature |
|-------|---------|
| [#220](https://github.com/juvistr/draftspec/issues/220) | TAP v13 formatter |
| [#221](https://github.com/juvistr/draftspec/issues/221) | TeamCity service messages |
| [#209](https://github.com/juvistr/draftspec/issues/209) | `--slow` flag for threshold highlighting |
| [#210](https://github.com/juvistr/draftspec/issues/210) | `--repeat` flag for flaky detection |
| [#212](https://github.com/juvistr/draftspec/issues/212) | `--seed` flag for deterministic ordering |

### v0.9.3-Integrations: Platform & AI

| Issue | Feature |
|-------|---------|
| [#211](https://github.com/juvistr/draftspec/issues/211) | `draftspec lint` command |
| [#222](https://github.com/juvistr/draftspec/issues/222) | MCP `analyze_failure` tool |
| [#223](https://github.com/juvistr/draftspec/issues/223) | MCP `generate_spec_from_description` |
| [#224](https://github.com/juvistr/draftspec/issues/224) | MTP tag traits for IDE filtering |

---

## Tracking

All features through v0.7.0 were tracked in the [Static Parsing Epic (#185)](https://github.com/juvistr/draftspec/issues/185).

---

*Last updated: January 2026*
