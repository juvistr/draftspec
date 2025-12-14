# Building an expressive spec-first testing framework in .NET 10

**.NET 10's C# 14 features unlock genuinely new possibilities for RSpec-style testing DSLs**, but a critical gap exists: no actively maintained framework offers true RSpec/Jasmine expressiveness in .NET. The opportunity to build something bespoke has never been better. Extension members enable `result.Should.Be.True` syntax without method parentheses, **CallerArgumentExpression** automatically captures assertion expressions for meaningful failure messages, and source generators can eliminate reflection entirely for AOT-compatible test discovery. Meanwhile, **LightBDD remains the only actively maintained BDD option** while TUnit represents the future of the .NET testing platform with source generator-based discovery.

## C# 14 and .NET 10 features transform what's possible

The November 2025 release of .NET 10 (LTS through 2028) paired with C# 14 introduces several features that fundamentally change DSL design in C#.

**Extension members** represent the headline feature for testing DSLs. Unlike traditional extension methods, C# 14 enables extension _properties_, _static members_, and even _operators_ on existing types. This means fluent assertion APIs can finally achieve `result.Should.Be.True` syntax—using properties instead of method calls—matching Ruby's natural language feel more closely than ever before:

```csharp
extension<T>(T subject)
{
    public AssertionBuilder Should => new AssertionBuilder(subject);
}

extension<bool>(AssertionBuilder<bool>)
{
    public bool True => Subject == true ? true : throw new AssertionException();
}
```

**CallerArgumentExpression** (stable since C# 10, now mature) is the most critical feature for testing frameworks. It captures the source expression passed to a method as a string at compile time, eliminating the need for expression trees:

```csharp
public static void ShouldBe<T>(
    this T actual, T expected,
    [CallerArgumentExpression("actual")] string? actualExpr = null)
{
    if (!Equals(actual, expected))
        throw new AssertionException($"Expected {actualExpr} to be {expected}, but was {actual}");
}

// Usage: user.Age.ShouldBe(25)
// Failure: "Expected user.Age to be 25, but was 30"
```

**Source generators with interceptors** enable reflection-free test discovery. The `.NET 10` SDK stabilized interceptors (previously experimental), allowing compile-time method call substitution. Combined with incremental source generators using `ForAttributeWithMetadataName` for **99x better performance** than `CreateSyntaxProvider`, spec definitions can be discovered and runner code generated without any runtime reflection—critical for Native AOT scenarios where TUnit already demonstrates 10-200x performance improvements over reflection-based frameworks.

Additional features worth leveraging include **params collections** (enabling `params Span<T>` for zero-allocation variadic methods), **partial constructors** (letting source generators provide initialization code while developers declare the shape), **collection expressions** with spread operators for test data composition, and **System.Linq.AsyncEnumerable** now built into the runtime for streaming test results.

## The BDD framework landscape has significant gaps

The current state of spec-style frameworks in .NET reveals **no actively maintained RSpec equivalent**, creating an opportunity for a bespoke solution.

| Framework | Status          | Last Activity      | Verdict                               |
| --------- | --------------- | ------------------ | ------------------------------------- |
| NSpec     | ⚠️ Stagnant     | 2017-2018          | Closest to RSpec syntax but abandoned |
| MSpec     | ⚠️ Low activity | Dec 2024           | Existing projects only                |
| LightBDD  | ✅ Active       | March 2025 (v3.11) | **Best maintained option**            |
| SpecFlow  | ❌ EOL          | Dec 31, 2024       | Migrate to Reqnroll                   |
| Reqnroll  | ✅ Active       | 2025               | **Best for Gherkin BDD**              |
| TUnit     | ✅ Active       | 2025               | **Future of .NET testing**            |

**NSpec** was the closest to RSpec syntax with its `describe/context/it` hierarchy and lambda-based DSL, supporting pending specs via `xit[]` prefix and `todo[]` keyword. However, it targets .NET Standard 1.6 and hasn't seen significant development in years. Its syntax remains instructive:

```csharp
class describe_Calculator : nspec
{
    void when_adding_numbers()
    {
        context["given two positive numbers"] = () =>
        {
            it["returns the sum"] = () => calc.Add(2, 3).should_be(5);
        };

        xit["not yet implemented"] = () => { }; // Pending
    }
}
```

**LightBDD** (v3.11, March 2025) is the most complete actively maintained option, offering Given-When-Then syntax with built-in HTML report generation and integrations with NUnit, xUnit, MSTest, Fixie, and now TUnit. It handles step status tracking (Passed, Failed, Bypassed, Ignored, NotRun) and provides real-time progress notification. However, it's fundamentally a Given-When-Then framework, not RSpec-style nesting.

**TUnit** emerged as the next-generation test framework built on `Microsoft.Testing.Platform` rather than VSTest. Using source generators instead of reflection, it achieves **10-200x faster discovery** in benchmarks, supports Native AOT, and defaults to parallel execution. While not a BDD framework itself, its architecture makes it an ideal foundation for building a spec layer on top.

The **pending spec gap** varies significantly: MSpec elegantly treats unassigned `It should_something;` fields as "Not Implemented", NSpec supports `xit[]` and `todo[]`, but LightBDD relies on framework-specific ignore mechanisms. No framework offers RSpec's elegant `it("should work")` without body for pending specs alongside `fit`/`fdescribe` for focus.

## File watching and REPL tooling enable fast feedback loops

Modern .NET provides robust tooling for the "code-save-see results" workflow essential to spec-first development.

**dotnet watch test** is the built-in solution, supporting filter expressions, hot reload integration, and `.NET 10` improvements including terminal logger formatting and parallel execution across target frameworks:

```bash
dotnet watch test --filter "FullyQualifiedName~UserSpecs"
```

Configure watched files via `<Watch Include="**\*.cs" />` in project files, and use `DOTNET_USE_POLLING_FILE_WATCHER=true` for Docker or network shares where FileSystemWatcher fails.

**CSharpRepl** (github.com/waf/CSharpRepl) stands out as the recommended REPL for interactive spec development. It supports `#r "path/to/specs.csproj"` for loading spec projects, NuGet package references, syntax highlighting, IntelliSense, and even Source Link navigation to view framework source code. Unlike .NET Interactive notebooks, it's optimized for CLI workflows.

**Hot Reload** in .NET 10 supports most method body changes, lambdas, adding methods/fields to existing types, and C# 10+ features. The `MetadataUpdateHandler` attribute enables custom behavior when code changes:

```csharp
[assembly: MetadataUpdateHandler(typeof(SpecHotReloadHandler))]
public static class SpecHotReloadHandler
{
    public static void UpdateApplication(Type[]? types)
    {
        SpecRunner.RerunAffectedSpecs(types);
    }
}
```

Known limitations include inability to change type definitions, add await/yield to existing methods, or modify method signatures—"rude edits" that require rebuild. Community reports indicate reliability issues in .NET 9/10, particularly with Blazor Server.

For **building custom file watchers**, combine `FileSystemWatcher` with Rx.NET for elegant debouncing:

```csharp
Observable.Using(
    () => new FileSystemWatcher(path, "*.cs") { EnableRaisingEvents = true },
    watcher => Observable.Merge(
        Observable.FromEventPattern<FileSystemEventArgs>(watcher, "Changed"),
        Observable.FromEventPattern<FileSystemEventArgs>(watcher, "Created")
    )
    .Select(e => e.EventArgs.FullPath)
    .Quiescent(TimeSpan.FromMilliseconds(100))
);
```

**Spectre.Console** provides rich terminal output for test results—colored status indicators, progress bars, tables—making it the obvious choice for CLI test watcher UX.

## Documentation generation options exist but require assembly

Living documentation generation has been disrupted by **SpecFlow's end-of-life** (December 31, 2024). LivingDoc was closed-source and cannot be forked, leaving Reqnroll users to rely on Allure Report or Pickles as alternatives.

**Available formats and tools:**

| Tool                | Status        | Best For           | Output                  |
| ------------------- | ------------- | ------------------ | ----------------------- |
| LightBDD Reports    | ✅ Active     | C# BDD             | HTML, XML, Text         |
| Allure Report       | ✅ Active     | Rich visualization | HTML with filtering     |
| Pickles             | ✅ Maintained | Static living docs | HTML, Word, Excel, JSON |
| ReportGenerator     | ✅ Active     | Coverage           | HTML, Markdown          |
| dorny/test-reporter | ✅ Active     | GitHub Actions     | Check runs              |

**LightBDD's built-in reports** show step status tracking, feature summaries, and execution timelines directly from C# test runs. For Gherkin-based specs, **Pickles** (v4.0.3) generates static documentation integrating test results for pass/fail visualization per scenario.

For CI integration, **TRX format** is native to `dotnet test`, while **trx2junit** (10.9M NuGet downloads) converts to JUnit XML for cross-platform CI systems. **dorny/test-reporter** GitHub Action parses TRX files and creates inline annotations on failures.

**Building custom documentation** is feasible using:

- **Markdig** for CommonMark Markdown processing
- **Scriban** for fast, liquid-compatible templating
- Parse TRX/JUnit XML with `System.Xml.Linq`
- Correlate results with spec definitions
- Output to static HTML or Markdown for GitHub Pages

Progress tracking for pending specs requires custom implementation—parse test results for `Skipped`/`Inconclusive` outcomes and generate summary statistics showing "15/20 specs passing, 3 pending, 2 failing."

## DSL patterns for RSpec-like syntax in modern C#

Building an expressive spec DSL requires combining several patterns with modern C# features.

**Lambda-based describe/context/it hierarchy** captures nested structure through closures:

```csharp
public abstract class Spec
{
    protected void describe(string description, Action body)
    {
        var context = new SpecContext(description);
        using (new ContextScope(context)) { body(); }
    }

    protected void it(string description, Action assertion)
        => CurrentContext.AddSpec(new SpecDefinition(description, assertion));

    // Pending spec - no body
    protected void it(string description)
        => CurrentContext.AddSpec(new SpecDefinition(description, null, isPending: true));

    // Focus and skip
    protected void fit(string description, Action assertion)
        => CurrentContext.AddSpec(new SpecDefinition(description, assertion, isFocused: true));

    protected void xit(string description, Action? assertion = null)
        => CurrentContext.AddSpec(new SpecDefinition(description, assertion, isSkipped: true));

    // Hooks as property setters
    protected Action before { set => CurrentContext.BeforeEach = value; }
    protected Action after { set => CurrentContext.AfterEach = value; }
}
```

**Fluent assertion chaining** uses `AndConstraint<T>` wrapper pattern (FluentAssertions style):

```csharp
public class AndConstraint<T> where T : class
{
    public T And { get; }
    public AndConstraint(T parent) => And = parent;
}

// Enables: result.Should().Be(5).And.BePositive().And.BeLessThan(10)
```

**Async support** requires overloaded methods accepting both `Action` and `Func<Task>`:

```csharp
protected void it(string description, Func<Task> assertion)
    => AddSpec(description, assertion);

protected void itAsync(string description, Func<Task> assertion)
    => AddSpec(description, assertion);
```

**Global usings** make DSL keywords available everywhere:

```csharp
// GlobalUsings.cs
global using static MyTestFramework.Dsl.Keywords;
global using MyTestFramework.Assertions;
```

**Fixie-style convention-based discovery** offers maximum flexibility through `IDiscovery` and `IExecution` interfaces, allowing spec classes to be discovered by naming convention or base class rather than attributes.

## Recommendations for building a bespoke framework

Given the gap in actively maintained RSpec-style frameworks, building a custom solution on modern foundations is justified.

**Architecture recommendation:**

1. **Foundation**: Build on **TUnit** or **xUnit** for test execution, parallelism, and IDE integration
2. **DSL layer**: Lambda-based `describe/context/it` with pending, focus, and skip support
3. **Assertions**: Extend **Shouldly** (MIT licensed; FluentAssertions v8+ requires commercial license) with CallerArgumentExpression
4. **Discovery**: Source generators for AOT compatibility, Fixie conventions for flexibility
5. **Documentation**: Custom generator using Scriban templates producing Markdown/HTML
6. **CLI**: Spectre.Console for rich terminal output with progress indicators

**Key implementation priorities:**

- **Pending specs**: Overloaded `it(string description)` without body parameter
- **Focus mechanism**: `fit`/`fdescribe` methods setting `isFocused` flag; runner skips non-focused when any exist
- **Let-style memoization**: Lazy<T> wrappers with per-spec reset in `beforeEach`
- **Hook ordering**: `beforeAll` → `beforeEach` (parent→child) → spec → `afterEach` (child→parent) → `afterAll`
- **Source location**: CallerFilePath/LineNumber attributes for click-to-source in error reports

**Minimal viable DSL target:**

```csharp
public class CalculatorSpec : Spec
{
    public CalculatorSpec()
    {
        describe("Calculator", () =>
        {
            Calculator calc = null!;
            before = () => calc = new Calculator();

            context("when adding", () =>
            {
                it("returns sum of two numbers", () =>
                    calc.Add(2, 3).ShouldBe(5));

                it("handles negative numbers"); // Pending

                xit("division edge cases"); // Skipped
            });
        });
    }
}
```

This approach leverages .NET 10's mature tooling while filling the genuine gap in the ecosystem—an actively maintained, expressive spec framework with first-class support for spec-first workflows where pending specs document intent before implementation exists.
