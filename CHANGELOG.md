# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `draftspec list` command for static spec discovery without execution (#186)
- ADR-007 documenting the declarative DSL model

### Changed

- CLI now uses in-process execution via `InProcessSpecRunner` (faster startup)
- MCP service uses new execution model with `SpecRunner` and `SpecReportBuilder`

### Removed

- **BREAKING:** `run()` removed from DSL - execution is now framework responsibility
- **BREAKING:** `configure()` removed from DSL - use CLI flags or `draftspec.json` instead
- **BREAKING:** `Dsl.Configuration.cs` deleted
- **BREAKING:** `SpecSession.RunnerBuilder` and `SpecSession.Configuration` properties removed

See [ADR-007](docs/adr/007-declarative-dsl-model.md) for rationale and migration guide.

## [0.4.0-alpha.6] - 2025-12-29

### Fixed

- Report compilation errors for requested specs in filtered runs (#184)
- Don't report non-requested specs during filtered runs (#183)
- Handle compilation errors in filtered test runs (#182)

## [0.4.0-alpha.3] - 2025-12-28

### Added

- Static CSX parsing for spec discovery from files with compilation errors (#181)

## [0.4.0-alpha.2] - 2025-12-28

### Fixed

- Use FailedTestNodeStateProperty for discovery errors in IDE (#180)

## [0.4.0-alpha.1] - 2025-12-28

### Added

- CancellationToken support in ISpecRunner interface (#173)
- Async overloads in Spec base class (#174)
- Custom middleware development guide (#175)

### Changed

- Introduced SpecSession abstraction to reduce static state (#177)
- Reduced expectation type explosion with composition pattern (#176)
- Unified formatter interface hierarchy (#172)
- Made SpecContext hook properties immutable (#171)

### Fixed

- Surface spec discovery errors through MTP as error nodes (#179)
- Resolve throw-lambda overload and thread-safe lazy init issues (#178)
- Add regex timeout to prevent ReDoS attacks in name filters (#160)
- Strengthen CSS sanitization with regex patterns (#161)

### Performance

- Cache FullDescription to avoid repeated string allocations (#165)
- Replace ImmutableList with push/pop pattern in SpecRunner (#164)

## [0.3.0-alpha.8] - 2025-12-27

### Fixed

- Simplify DisplayName and preserve spaces in identifiers (#141)

## [0.3.0-alpha.7] - 2025-12-27

### Fixed

- Add TestMethodIdentifierProperty for Rider IDE integration (#139)

## [0.3.0-alpha.6] - 2025-12-27

### Fixed

- Add missing MSBuild properties for IDE protocol support (#138)

## [0.3.0-alpha.5] - 2025-12-27

### Fixed

- Make timeout middleware test more reliable with NotInParallel (#137)

## [0.3.0-alpha.3] - 2025-12-27

### Fixed

- Make timeout middleware tests more reliable

## [0.3.0-alpha.2] - 2025-12-27

### Fixed

- Enable packaging for DraftSpec.TestingPlatform

## [0.3.0-alpha.1] - 2025-12-27

### Added

- Microsoft Testing Platform (MTP) adapter for `dotnet test` integration (#125-#130)
- Roslyn-based CsxScriptHost for in-process CSX execution (#126)
- SpecDiscoverer for MTP test enumeration (#127)
- Filtered spec execution for MTP (#128)
- IDE navigation support with line number capture (#129)
- VSTestBridge support for IDE integration (#135)
- MTP integration documentation and examples (#130)

## [0.2.2-alpha.1] - 2025-12-26

### Changed

- Use dotnet-coverage server mode for efficient multi-file coverage (#117)

## [0.2.1-alpha.1] - 2025-12-26

### Added

- Code coverage support with `--coverage` flag (#115)

## [0.2.0-alpha.1] - 2025-12-25

### Added

- Table-driven test support with `withData()` (#108)
- Snapshot testing support with `toMatchSnapshot()` (#110)
- `draftspec.json` configuration file support (#111)
- Natural language assertion helper (parse_assertion MCP tool) (#112)
- JUnit XML output format for CI/CD integration (#94)
- `--bail` flag to stop execution after first failure (#93)
- `--filter` and `--exclude` CLI flags for tag/name filtering (#95)
- `.not` negation pattern for assertions (#91)
- `toThrowAsync()` for async action expectations (#89)
- `toMatch()` and `toHaveLength()` string assertions (#88)
- Additional collection assertions (#92)
- MCP resources for spec file access (#56)
- Webhook Reporter for external notifications (#55)
- In-process spec execution mode using Roslyn scripting (#51)
- Batch execution tool for multiple spec files (#49)
- Spec diff tool for regression detection (#50)
- Session persistence for multi-turn agent workflows (#45)
- MCP progress notifications for streaming feedback (#44)
- Structured error taxonomy for AI parsing (#48)

### Changed

- Unified SpecReport types across Core and MCP (#109)
- Centralized JsonSerializerOptions configuration (#37)
- Enable dotnet-script caching by default, add `--no-cache` flag
- Extract IEnvironmentProvider abstraction for testability (#99)

### Fixed

- Exit code not being reset to 0 on successful spec runs (#106)
- O(n²) hook chain building in SpecContext (#32)
- Multiple IEnumerable enumeration in CollectionExpectation (#30)
- Scaffolder string escaping to prevent code injection (#27)
- Path validation to prevent path traversal attacks (#28)

### Performance

- Reduce allocations in SpecRunner hot paths (#36)
- Return singleton empty collections for contexts without hooks (#96)
- Lazy-allocate SpecExecutionContext.Items (#97)
- Cache focus/spec count on tree construction (#98)
- LRU eviction for script cache (#101)
- Use HashCode instead of SHA256 for script cache keys (#100)

### Security

- Strengthen session ID generation with cryptographic randomness (#90)
- Add ScaffoldNode depth limit to prevent stack overflow (#33)
- Document MCP trust model in SECURITY.md (#29)

## [0.1.0-alpha.4] - 2025-12-20

Initial prerelease with core functionality.

### Added

- RSpec-style DSL (`describe`, `context`, `it`, `fit`, `xit`)
- Jest-style assertions (`expect().toBe()`, `toBeNull()`, `toContain()`, etc.)
- Hooks (`before`, `after`, `beforeAll`, `afterAll`)
- Focus and skip support (`fit`, `xit`, `fdescribe`, `xdescribe`)
- Tags and filtering
- Middleware pipeline for cross-cutting concerns
- Plugin system (formatters, reporters)
- CLI tool (`draftspec run`, `draftspec watch`, `draftspec init`, `draftspec new`)
- Output formats: console, JSON, HTML, Markdown
- Parallel spec file execution with `--parallel` flag
- MCP server for AI-assisted testing
- NuGet package configuration

[Unreleased]: https://github.com/jvstr/draftspec/compare/v0.4.0-alpha.6...HEAD
[0.4.0-alpha.6]: https://github.com/jvstr/draftspec/compare/v0.4.0-alpha.3...v0.4.0-alpha.6
[0.4.0-alpha.3]: https://github.com/jvstr/draftspec/compare/v0.4.0-alpha.2...v0.4.0-alpha.3
[0.4.0-alpha.2]: https://github.com/jvstr/draftspec/compare/v0.4.0-alpha.1...v0.4.0-alpha.2
[0.4.0-alpha.1]: https://github.com/jvstr/draftspec/compare/v0.3.0-alpha.8...v0.4.0-alpha.1
[0.3.0-alpha.8]: https://github.com/jvstr/draftspec/compare/v0.3.0-alpha.7...v0.3.0-alpha.8
[0.3.0-alpha.7]: https://github.com/jvstr/draftspec/compare/v0.3.0-alpha.6...v0.3.0-alpha.7
[0.3.0-alpha.6]: https://github.com/jvstr/draftspec/compare/v0.3.0-alpha.5...v0.3.0-alpha.6
[0.3.0-alpha.5]: https://github.com/jvstr/draftspec/compare/v0.3.0-alpha.3...v0.3.0-alpha.5
[0.3.0-alpha.3]: https://github.com/jvstr/draftspec/compare/v0.3.0-alpha.2...v0.3.0-alpha.3
[0.3.0-alpha.2]: https://github.com/jvstr/draftspec/compare/v0.3.0-alpha.1...v0.3.0-alpha.2
[0.3.0-alpha.1]: https://github.com/jvstr/draftspec/compare/v0.2.2-alpha.1...v0.3.0-alpha.1
[0.2.2-alpha.1]: https://github.com/jvstr/draftspec/compare/v0.2.1-alpha.1...v0.2.2-alpha.1
[0.2.1-alpha.1]: https://github.com/jvstr/draftspec/compare/v0.2.0-alpha.1...v0.2.1-alpha.1
[0.2.0-alpha.1]: https://github.com/jvstr/draftspec/compare/v0.1.0-alpha.4...v0.2.0-alpha.1
[0.1.0-alpha.4]: https://github.com/jvstr/draftspec/releases/tag/v0.1.0-alpha.4
