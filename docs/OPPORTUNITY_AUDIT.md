# DraftSpec Opportunity Audit

This document captures all identified enhancement opportunities from the December 2025 codebase audit. Use as a reference for future roadmap planning.

---

## Summary

| Category | Opportunities | v0.9.0 Issues | Future |
|----------|---------------|---------------|--------|
| CLI | 25+ | 4 | 21+ |
| Assertions | 25+ | 4 | 21+ |
| DSL/Hooks | 12 | 4 | 8 |
| Formatters | 17 | 2 | 15 |
| MTP/IDE | 12 | 1 | 11 |
| MCP | 12 | 2 | 10 |
| **Total** | ~100 | **17** | ~83 |

---

## v0.9.0 Issues Created

### CLI
- [#209](https://github.com/juvistr/draftspec/issues/209) - `--slow <ms>` flag
- [#210](https://github.com/juvistr/draftspec/issues/210) - `--repeat <n>` flag
- [#211](https://github.com/juvistr/draftspec/issues/211) - `draftspec lint` command
- [#212](https://github.com/juvistr/draftspec/issues/212) - `--seed` flag

### Assertions
- [#213](https://github.com/juvistr/draftspec/issues/213) - `toBeOneOf()` / `toSatisfy()`
- [#214](https://github.com/juvistr/draftspec/issues/214) - DateTime matchers
- [#215](https://github.com/juvistr/draftspec/issues/215) - `.because()` contextual messages
- [#216](https://github.com/juvistr/draftspec/issues/216) - Collection matchers

### DSL/Hooks
- [#217](https://github.com/juvistr/draftspec/issues/217) - Multiple hooks per context
- [#218](https://github.com/juvistr/draftspec/issues/218) - `let()` lazy fixtures
- [#219](https://github.com/juvistr/draftspec/issues/219) - Around hooks
- [#225](https://github.com/juvistr/draftspec/issues/225) - Shared examples

### Formatters
- [#220](https://github.com/juvistr/draftspec/issues/220) - TAP formatter
- [#221](https://github.com/juvistr/draftspec/issues/221) - TeamCity reporter

### MTP/IDE
- [#224](https://github.com/juvistr/draftspec/issues/224) - Tag traits for IDE filtering

### MCP
- [#222](https://github.com/juvistr/draftspec/issues/222) - `analyze_failure` tool
- [#223](https://github.com/juvistr/draftspec/issues/223) - `generate_spec_from_description`

---

## Full Opportunity Catalog

### CLI Opportunities

#### Commands

| Opportunity | Description | Priority | Effort |
|-------------|-------------|----------|--------|
| `draftspec lint` | Check specs for anti-patterns | HIGH | Medium |
| `draftspec graph` | Visualize `#load` dependencies (dot/mermaid) | Medium | Medium |
| `draftspec debug` | Verbose single-spec execution | Medium | Medium |
| `draftspec status` | Health check without running tests | Low | Low |
| `draftspec compare` | Diff baseline vs current results | Medium | Medium |

#### Flags for `run`

| Flag | Description | Priority | Effort |
|------|-------------|----------|--------|
| `--slow <ms>` | Highlight specs exceeding threshold | HIGH | Low |
| `--repeat <n>` | Run specs N times for flaky detection | HIGH | Low |
| `--seed <n>` | Deterministic spec ordering | Medium | Low |
| `--timeout <ms>` | Per-invocation timeout override | Medium | Low |
| `--fail-fast <n>` | Stop after N failures | Low | Low |
| `--env KEY=val` | Set environment variables | Low | Low |

#### Flags for `watch`

| Flag | Description | Priority | Effort |
|------|-------------|----------|--------|
| `--trigger <pattern>` | Narrow file watch to specific glob | Medium | Low |
| `--mode changed` | Re-run only changed specs | Medium | Medium |

#### Configuration

| Feature | Description | Priority | Effort |
|---------|-------------|----------|--------|
| `performance` block | slowThreshold, reportSlowTests | Medium | Low |
| `watch` block | triggerPatterns, debounceMs, mode | Medium | Low |
| `discovery` block | ignorePatterns, followSymlinks | Low | Low |

#### DX Improvements

| Feature | Description | Priority | Effort |
|---------|-------------|----------|--------|
| Smart command suggestions | "Did you mean: run?" | Low | Low |
| `--explain` flag | Remediation hints on failures | Low | Medium |
| Progress bar with ETA | During execution | Low | Low |
| `--generate-config` | Auto-generate draftspec.json | Low | Medium |

---

### Assertion Opportunities

#### Generic Matchers

| Matcher | Description | Priority | Effort |
|---------|-------------|----------|--------|
| `toBeOneOf([a,b,c])` | Value is in set | HIGH | Low |
| `toSatisfy(predicate)` | Custom predicate | HIGH | Low |
| `toBeTruthy/toBeFalsy` | Boolean coercion | Medium | Low |
| `toHaveProperty(name)` | Object has property | Medium | Low |
| `.because("reason")` | Contextual failure message | HIGH | Low |

#### DateTime Matchers

| Matcher | Description | Priority | Effort |
|---------|-------------|----------|--------|
| `toBeAfter(date)` | Temporal comparison | HIGH | Medium |
| `toBeBefore(date)` | Temporal comparison | HIGH | Medium |
| `toBeWithin(span).of(date)` | Tolerance-based | Medium | Medium |
| `toBeToday()` | Date is today | Low | Low |
| `toBeInThePast/Future()` | Relative to now | Low | Low |

#### Numeric Matchers

| Matcher | Description | Priority | Effort |
|---------|-------------|----------|--------|
| `toBePositive/Negative/Zero` | Sign classification | Medium | Low |
| `toBeNaN/toBeFinite` | Floating point edge cases | Low | Low |
| `toBeEven/toBeOdd` | Parity check | Low | Low |

#### String Matchers

| Matcher | Description | Priority | Effort |
|---------|-------------|----------|--------|
| `toBeUpperCase/LowerCase` | Case validation | Low | Low |
| `toBeAlphanumeric` | Character validation | Low | Low |
| `toBeValidEmail/Url/Guid` | Format validation | Low | Medium |

#### Collection Matchers

| Matcher | Description | Priority | Effort |
|---------|-------------|----------|--------|
| `toBeUnique()` | No duplicates | HIGH | Low |
| `toAllSatisfy(predicate)` | All items match | HIGH | Low |
| `toBeAscending/Descending` | Order verification | Medium | Low |
| `toContainNone([items])` | Negative containment | Medium | Low |
| `toContainKey/Value` | Dictionary matchers | Medium | Low |

#### Async Matchers

| Matcher | Description | Priority | Effort |
|---------|-------------|----------|--------|
| `toCompleteWithin(span)` | Task timeout | Medium | Medium |
| `toResolveWith(value)` | Task result | Medium | Medium |
| `toReject<TException>()` | Task failure | Medium | Medium |

#### Infrastructure

| Feature | Description | Priority | Effort |
|---------|-------------|----------|--------|
| Matcher registry/aliases | Custom matcher names | Low | Medium |
| Assertion counting | `expect.assertions(3)` | Low | Medium |
| Better collection diffs | Side-by-side output | Low | Medium |

---

### DSL/Hook Opportunities

| Feature | Description | Priority | Effort |
|---------|-------------|----------|--------|
| Multiple hooks per context | Accumulate vs overwrite | HIGH | Low |
| `let()` lazy fixtures | Memoized per-spec values | HIGH | Medium |
| Around hooks | Wrap spec execution | HIGH | High |
| Shared examples | Reusable test patterns | HIGH | High |
| Conditional hooks | `before_if(condition, ...)` | Medium | Medium |
| Hook metadata | Description/purpose fields | Low | Low |
| Hooks with context access | Inspect spec metadata | Medium | Medium |
| Hook ordering guarantees | Explicit dependencies | Low | High |
| Context-local lazy values | Scoped fixture isolation | Low | High |
| Hook retry/recovery | Resilient setup/teardown | Low | Medium |
| Fixture cleanup scope | `using_fixture<T>` pattern | Medium | High |
| Dynamic spec generation | Runtime tree mutation | Low | Very High |

---

### Formatter/Reporter Opportunities

#### Output Formats

| Format | Description | Priority | Effort |
|--------|-------------|----------|--------|
| TAP v13 | Test Anything Protocol | HIGH | Low |
| TeamCity | Service messages | HIGH | Medium |
| NUnit 3 XML | .NET standard format | Medium | Medium |
| TRX | Visual Studio format | Medium | High |
| Sonar Generic | SonarQube integration | Low | Medium |

#### Reporters

| Reporter | Description | Priority | Effort |
|----------|-------------|----------|--------|
| Progress bar | ETA and percentage | Medium | Low |
| Delta/diff | Compare to baseline | Medium | Medium |
| NDJSON streaming | Log aggregation | Low | Low |
| Database | Historical storage | Low | High |
| OpenTelemetry | Observability export | Low | High |

#### Infrastructure

| Feature | Description | Priority | Effort |
|---------|-------------|----------|--------|
| Custom templates | Liquid/Handlebars for HTML/MD | Medium | High |
| Formatter options pattern | Base class for config | Medium | Medium |
| Composite reporter | Chain multiple reporters | Low | Low |
| Async batch buffering | High-volume optimization | Low | Medium |

---

### MTP/IDE Opportunities

| Feature | Description | Priority | Effort |
|---------|-------------|----------|--------|
| Tag trait properties | IDE filtering by tag | HIGH | Medium |
| Coverage properties | Per-spec coverage in IDE | Medium | Medium |
| Flakiness indicators | Retry badge/property | Medium | Medium |
| Detailed timing breakdown | Hooks vs body timing | Low | Medium |
| Capability declarations | Announce MTP features | Medium | Low |
| Custom container nodes | "Slow Tests" grouping | Low | Medium |
| Discovery progress | Incremental feedback | Low | Medium |
| Configuration export | Expose settings to IDE | Low | Low |

---

### MCP Opportunities

| Tool | Description | Priority | Effort |
|------|-------------|----------|--------|
| `analyze_failure` | AI-assisted debugging | HIGH | Medium |
| `generate_spec_from_description` | NL to spec | HIGH | Medium |
| `validate_spec_patterns` | Pattern checking | Medium | Low |
| `track_baseline` | Result snapshots | Medium | Medium |
| `suggest_next_spec` | Context-aware suggestions | Medium | Medium |
| `generate_specs_from_type` | Reflection-based scaffolding | Medium | High |
| `profile_spec_execution` | Performance analysis | Low | Medium |
| `lint_specs` | Quality checks via MCP | Low | Low |
| `generate_implementation_from_spec` | TDD red-green | Low | High |
| `assertion_matrix_generator` | Boundary testing | Low | Medium |
| Help resources | Best practices for AI agents | Low | Low |

---

## Priority Matrix

### Quick Wins (High Impact, Low Effort)

1. `--slow <ms>` flag
2. `--repeat <n>` flag
3. `toBeOneOf()` / `toSatisfy()`
4. `.because()` contextual messages
5. Multiple hooks per context
6. TAP formatter
7. Hook metadata/descriptions

### Strategic (High Impact, Medium-High Effort)

1. `let()` lazy fixtures
2. DateTime matchers
3. Around hooks
4. Shared examples
5. TeamCity reporter
6. MCP `analyze_failure`

### Future Consideration

1. TRX formatter (Azure DevOps)
2. Database reporter (historical analysis)
3. OpenTelemetry export
4. Dynamic spec generation
5. Custom formatter templates

---

## Audit Methodology

This audit was conducted by 6 parallel exploration agents examining:

1. **CLI** (`src/DraftSpec.Cli/`) - Commands, flags, configuration
2. **MTP/IDE** (`src/DraftSpec.TestingPlatform/`) - Test node mapping, properties
3. **Assertions** (`src/DraftSpec/Expectations/`) - Matchers, error messages
4. **Formatters** (`src/DraftSpec.Formatters.*/`, `src/DraftSpec/Plugins/`) - Output formats
5. **DSL/Hooks** (`src/DraftSpec/Dsl*.cs`, `SpecContext.cs`) - Language features
6. **MCP** (`src/DraftSpec.Mcp/`) - AI-native tooling

Comparisons made against: Jest, RSpec, pytest, FluentAssertions, Chai.

---

*Last updated: December 2025*
*Audit version: 1.0*
