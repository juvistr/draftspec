# Spec-first development patterns beyond traditional testing

Declaring intent before implementation is a solved problem in multiple domains—but with fragmented tooling and no unified paradigm. **Typed holes in functional languages**, **API coverage tracking in schema-first development**, and **drift detection in infrastructure-as-code** represent the most mature patterns that could transform spec-first testing in C#/.NET. The key insight: the best systems make "incomplete" a first-class, trackable state rather than just a runtime error.

## Typed holes represent the gold standard for intent-before-implementation

Functional programming languages have evolved the most sophisticated mechanisms for expressing "this should exist but doesn't yet." **Haskell's typed holes** (since GHC 7.8.1) allow placeholders written with underscores (`_`, `_foo`) that trigger compiler errors revealing the expected type, available bindings in scope, and suggested completions. The `-fdefer-typed-holes` flag converts these to warnings, allowing incomplete code to compile while remaining trackable.

**Idris and Agda** take this further with interactive hole-driven development. The REPL command `:m` lists all unproven holes with their qualified names and types—essentially a "spec coverage report" built into the compiler. Agda's `C-c C-a` command attempts automatic proof search to fill holes, while Idris's `:ps` performs similar proof search based on types. This creates a three-way conversation: programmer, IDE, and compiler collaborating to complete specifications.

| Language   | Pending Signal   | Tracking Mechanism                   |
| ---------- | ---------------- | ------------------------------------ |
| Haskell    | `_` or `_name`   | GHC errors list all holes with types |
| Idris      | `?name`          | `:m` command lists all holes         |
| Agda       | `?` or `{! !}`   | Emacs goals buffer                   |
| Scala      | `???`            | Returns `Nothing`, IDE tracking      |
| Rust       | `todo!()`        | IDE highlights, runtime panic        |
| TypeScript | `assertNever(x)` | Compile error on unhandled cases     |

For C#, `NotImplementedException` provides a weak equivalent—it compiles and throws at runtime, but there's no built-in mechanism to enumerate all instances across a codebase. **Roslyn analyzers could surface these**, but no standard analyzer exists. Source generators could create a richer system where skeleton methods are generated from interface declarations with trackable "hole" states.

## API-first tooling has solved implementation coverage tracking

Schema-first API development has evolved mature tooling for the exact problem of tracking "spec exists, implementation pending." **swagger-coverage** generates reports categorizing endpoints as Empty/Partial/Full coverage based on test execution against OpenAPI specs. **Specmatic** detects mismatches between OpenAPI specifications and actual implementations, integrating into CI/CD pipelines to fail builds when drift occurs.

The pattern works like this: write OpenAPI spec first, generate server stubs (via OpenAPI Generator or NSwag for .NET), and stubs return **501 Not Implemented** by default. Coverage tools then track which stubs have real implementations versus which still throw. **TraceCov** provides browser-based visualization of coverage gaps across endpoints, parameters, and response schemas.

**Prism** from Stoplight deserves special attention—it generates mock servers from OpenAPI specs, enabling frontend development before backend implementation. Its validation proxy mode can run alongside real implementations to continuously verify conformance. This creates a development workflow where the spec is authoritative and tooling enforces it.

GraphQL's ecosystem offers parallel patterns. **GraphQL Code Generator** with `avoidOptionals: true` forces all resolvers to be implemented—the TypeScript compiler errors on missing resolvers rather than allowing optional undefined behavior. This transforms type generation into a spec enforcement mechanism.

## Design-by-contract tools exist but require verification-aware languages

**Microsoft's Code Contracts library was archived in July 2023**, leaving .NET without official design-by-contract support. The pattern pioneered by Eiffel—preconditions, postconditions, and invariants as first-class language constructs—never gained mainstream .NET adoption. However, **Dafny** from Microsoft Research/Amazon offers an alternative path: a verification-aware programming language that compiles to C#, Go, Python, Java, and JavaScript.

Dafny bakes contracts into the language with preconditions, postconditions, loop invariants, and frame specifications. The Z3 SMT solver verifies contracts during development, not at runtime. Amazon's Automated Reasoning group actively develops Dafny, and it's seeing adoption in security-critical code generation.

For TypeScript, Python, and Java, active contract libraries exist:

- **deal** (Python): Full design-by-contract with static analysis, formal verification, and pytest integration
- **icontract** (Python): CrossHair integration for automatic verification, FastAPI support
- **ts-code-contracts** (TypeScript): `requires`/`ensures`/`checks` assertions with type narrowing
- **decorator-contracts** (TypeScript): Liskov principle enforcement via decorators

**SPARK/Ada** represents the industrial-strength approach—contracts as language-level aspects verified by GNATprove using theorem provers (CVC4, Z3, Alt-Ergo). NVIDIA uses SPARK for security-critical firmware, demonstrating that spec-first with verification scales to production systems.

## Literate programming and documentation-driven development are converging with AI

Traditional literate programming tools like **Org-mode's org-babel** support spec-first through TODO states on code blocks and bidirectional tangling (`:comments link` inserts source links, `org-babel-detangle` syncs changes back). **Quarto** enables document-first development where `#| eval: false` marks code as specified but not executed.

The significant 2024-2025 development is **GitHub Spec Kit**—an open-source CLI for spec-driven development with AI. It creates specification files that AI coding assistants use to generate implementations. The workflow: write specs in `.spec/` directory with YAML metadata including `status: planned | in-progress | complete`, then AI assistants (Claude Code, Cursor) generate implementations that link back to specs.

**Marimo** (2024) represents a new notebook paradigm—reactive Python notebooks stored as pure Python files with no hidden state. Cells marked as stale rather than auto-executing enable lazy, spec-first workflows. Unlike Jupyter's execution order problems, Marimo's dependency graph ensures reproducibility.

**Rust's doctests** provide the gold standard for documentation-code synchronization: code blocks in `///` doc comments are automatically compiled and tested by `cargo test`. Annotations like `ignore`, `should_panic`, `no_run`, and `compile_fail` let you express "documented but intentionally not implemented" states.

## Planning-to-code bridges automate the spec-tracking lifecycle

**TODO-to-Issue tools** provide lightweight spec-first patterns within existing codebases. GitHub Actions like **TODO to Issue** convert structured TODO comments into GitHub Issues on push, auto-closing when TODOs are removed. Comments support metadata: labels, milestones, assignees, and multi-line bodies.

```python
# TODO: Implement caching layer
# body: Need to add Redis caching for expensive queries
# labels: enhancement, performance
# milestone: v3.0
```

**Architecture Decision Records (ADRs)** track design intent with status fields (proposed → accepted → deprecated → superseded) and links to implementation code. The **MADR template** is most popular, and tools like **Log4brains** and **Backstage ADR Plugin** provide searchable interfaces across repositories. Azure Well-Architected Framework officially features ADRs as of October 2024.

**Feature flags represent spec-first in disguise**—flag existence declares intent, implementation lives behind the flag, and rollout percentage tracks completion. LaunchDarkly, Unleash, and Harness provide lifecycle tracking from "flag created" through "flag removed after full rollout." Martin Fowler's feature toggle categories (release, experiment, ops, permission) map cleanly to different spec-first use cases.

## Terraform drift detection models spec-vs-reality tracking

Infrastructure-as-code provides a compelling pattern: **desired state** in `.tf` configuration files versus **actual state** in `terraform.tfstate`, with **drift** representing divergence. `terraform plan -detailed-exitcode` returns exit codes distinguishing "no changes needed" from "drift detected."

Tools like **driftctl** (open-source) and **Spacelift** provide continuous drift monitoring with alerts. This pattern translates directly to spec-first testing: spec files define desired behavior, implementation represents actual behavior, and tooling continuously monitors for drift.

**Database migrations** (Flyway, Liquibase) implement similar tracking. Each migration has states: pending, applied, failed, outdated. `flyway check -drift` and `liquibase diff` detect when actual schema diverges from expected. The `flyway_schema_history` table serves as an audit log of spec-to-implementation transitions.

## Domain-specific patterns offer additional inspiration

**Game development** uses Game Design Documents (GDDs) as living specifications. Tools like **Nuclino** and **Drafft** support collaborative docs with dialogue trees, item databases, and JSON export for integration. The pattern: narrative spec → technical spec → implementation, with continuous iteration. Unity's Scriptable Objects and Unreal's Blueprints enable designers to specify behavior before C++ implementation.

**Embedded systems** require formal requirements traceability for safety certification (ISO 26262, DO-178C). **IBM DOORS** and **Jama Connect** track bidirectional relationships: requirements → design → implementation → test. While heavyweight, these tools demonstrate how regulated industries solve the spec-tracking problem at scale.

## Patterns that could inspire C#/.NET spec-first testing

Several approaches could transform spec-first testing in .NET:

1. **Roslyn analyzer for NotImplementedException tracking** - Surface all instances in a single view, like Haskell's hole listing. Include source location, expected signature, and time since creation.

2. **Source generators for spec skeletons** - From interface declarations or attribute-marked specs, generate method stubs with `[Pending]` attributes that Roslyn tracks and reports.

3. **Swagger-coverage-style test reporting** - Map test assertions to spec declarations, showing coverage as Empty/Partial/Full per spec item.

4. **Drift detection for specs** - Continuous monitoring that alerts when implementation behavior diverges from spec declarations, similar to Terraform's approach.

5. **TODO-to-Test integration** - Structured comments that generate test stubs with pending states, synced to GitHub Issues for lifecycle tracking.

6. **Feature-flag-inspired spec states** - Specs with rollout-style states (defined, implementing, verifying, complete, deprecated) and dashboards showing completion percentage.

The key insight across all domains: **the best spec-first systems treat "incomplete" as a trackable state with tooling support, not merely a runtime error or comment**. Whether through typed holes, API coverage metrics, or drift detection, mature systems make the gap between intent and implementation visible, measurable, and actionable.

## Conclusion

The spec-first problem has been solved multiple times in different contexts—what's missing is a unified approach for general-purpose languages like C#. **Typed holes** demonstrate that compilers can actively assist in tracking implementation gaps. **API coverage tools** prove that tooling can measure spec-to-implementation completion. **Drift detection** shows how to continuously monitor for divergence. The opportunity for .NET is to synthesize these patterns into a coherent spec-first testing framework where declarations are first-class, completion status is automatically tracked, and IDE integration makes pending work immediately visible.
