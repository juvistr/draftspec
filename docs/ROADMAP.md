# DraftSpec Roadmap

This roadmap tracks development priorities from the initial v0.4.0 release through future milestones.

---

## Current Status: v0.4.x (Production Ready)

**Released December 2025** - All foundational work complete.

| Area | Status | Highlights |
|------|--------|------------|
| Core DSL | Complete | `describe`/`it`/`expect` API, hooks, focus/skip/pending |
| Security | Complete | Path traversal protection, temp file hardening, JSON limits |
| Performance | Complete | Zero-allocation assertions, incremental builds, parallel execution |
| Architecture | Complete | DI container, plugin discovery, result streaming |
| Test Coverage | ~90% | 515+ tests across all modules |
| Static Parsing | Complete | Roslyn-based spec discovery from CSX files |
| MTP Integration | Complete | Microsoft.Testing.Platform support for IDE integration |

---

## v0.5.0: CLI Foundation

**Theme**: Core CLI commands leveraging static parsing for fast spec discovery.

| Issue | Feature | Priority |
|-------|---------|----------|
| [#186](https://github.com/juvistr/draftspec/issues/186) | `draftspec list` - Discover and display specs | HIGH |
| [#187](https://github.com/juvistr/draftspec/issues/187) | `draftspec validate --static` - Pre-execution validation | HIGH |
| [#188](https://github.com/juvistr/draftspec/issues/188) | Line number filtering (`file:line` syntax) | HIGH |
| [#190](https://github.com/juvistr/draftspec/issues/190) | Enhanced compilation error messages | HIGH |
| [#189](https://github.com/juvistr/draftspec/issues/189) | Context-level filtering (`--context` flag) | MEDIUM |
| [#191](https://github.com/juvistr/draftspec/issues/191) | JSON schema for discovery output | DOCS |

**Key Dependency**: #186 (`draftspec list`) unblocks all other v0.5.0 features.

---

## v0.6.0: CI/CD Integration

**Theme**: Features that optimize CI pipelines and development workflows.

| Issue | Feature | Priority |
|-------|---------|----------|
| [#192](https://github.com/juvistr/draftspec/issues/192) | Parallel test partitioning (`--partition`) | HIGH |
| [#193](https://github.com/juvistr/draftspec/issues/193) | Incremental watch mode (only re-run changed specs) | HIGH |
| [#194](https://github.com/juvistr/draftspec/issues/194) | Pre-flight validation for CI pipelines | MEDIUM |
| [#196](https://github.com/juvistr/draftspec/issues/196) | Spec count statistics before run | LOW |
| [#195](https://github.com/juvistr/draftspec/issues/195) | GitHub Actions workflow templates | DOCS |

---

## v0.7.0: Test Intelligence

**Theme**: Smart test selection and historical analysis.

| Issue | Feature | Priority |
|-------|---------|----------|
| [#197](https://github.com/juvistr/draftspec/issues/197) | Test impact analysis (`--affected-by`) | HIGH |
| [#198](https://github.com/juvistr/draftspec/issues/198) | Dependency graph from `#load` directives | MEDIUM |
| [#199](https://github.com/juvistr/draftspec/issues/199) | Flaky test detection and quarantine | MEDIUM |
| [#201](https://github.com/juvistr/draftspec/issues/201) | Result caching for watch mode | MEDIUM |
| [#200](https://github.com/juvistr/draftspec/issues/200) | Runtime estimation from historical data | LOW |

---

## v0.8.0: Future Enhancements

**Theme**: Advanced DX features and ecosystem expansion.

| Issue | Feature | Priority |
|-------|---------|----------|
| [#206](https://github.com/juvistr/draftspec/issues/206) | Enhanced MTP Test Explorer integration | MEDIUM |
| [#207](https://github.com/juvistr/draftspec/issues/207) | Spec authoring best practices guide | DOCS |
| [#202](https://github.com/juvistr/draftspec/issues/202) | Living documentation generation | LOW |
| [#203](https://github.com/juvistr/draftspec/issues/203) | Spec coverage mapping | LOW |
| [#204](https://github.com/juvistr/draftspec/issues/204) | Interactive spec selection (`--interactive`) | LOW |
| [#205](https://github.com/juvistr/draftspec/issues/205) | Rider/VS Code CodeLens plugins | LOW |

---

## Tracking

All features above are tracked in the [Static Parsing Epic (#185)](https://github.com/juvistr/draftspec/issues/185).

---

*Last updated: December 2025*
