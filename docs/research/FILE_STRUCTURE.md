# File-based testing DSLs in .NET 10: What's possible without classes

The dream of RSpec/Jasmine-style `describe`/`it` syntax in C# without class boilerplate faces fundamental constraints. **Source generators cannot transform inline DSL code** within .cs files—they're strictly additive. However, three viable paths emerged from .NET 10's November 2025 release: external DSL files processed by source generators (recommended), C# scripting with custom runners, or runtime interpretation patterns. The most production-ready approach combines custom file formats with Roslyn source generators.

## Top-level statements remain confined to a single file

Despite hopes that .NET 10 would extend top-level statements, **only one file per project can contain top-level code**. Attempting multiple files triggers error CS8802. This limitation exists because the compiler generates a synthetic `Program` class with an entry point method—and applications can only have one entry point.

.NET 10's headline "file-based apps" feature (`dotnet run app.cs`) also doesn't help. While it enables running single `.cs` files directly with elegant directives like `#:package Humanizer@2.14.1` and `#:sdk Microsoft.NET.Sdk.Web`, multi-file support is explicitly deferred to .NET 11. C# 14's new extension members (extension properties, operators, indexers) provide cleaner fluent APIs but don't address the core DSL challenge.

The `file` access modifier from C# 11 limits type visibility to a single file, but still requires explicit class declarations—no automatic class generation from file content exists.

## Source generators can't transform inline C# DSL syntax

A critical constraint for DSL design: **source generators are explicitly additive-only**. They cannot modify, replace, or transform existing source code. This means syntax like:

```csharp
describe("Calculator", () => {
    it("should add numbers", () => Assert.Equal(4, Add(2, 2)));
});
```

**Cannot work** in a .cs file—there's no mechanism for a source generator to transform those method calls into a test class. Interceptors (now stable in SDK 9.0.2xx+) only reroute existing method calls with exact file/line/column targeting; they don't parse or transform syntax.

However, source generators excel at processing **external DSL files**. Through the `AdditionalFiles` mechanism, generators can read YAML, Markdown, JSON, Gherkin, or custom formats and emit complete test classes:

```xml
<ItemGroup>
    <AdditionalFiles Include="**/*.spec.yaml" />
</ItemGroup>
```

A generator then transforms spec files into xUnit/NUnit/MSTest classes with full IDE integration—IntelliSense, navigation, compile-time error detection. This is Razor's exact pattern: .razor files enter as AdditionalFiles, the source generator parses them, and emits C# partial classes with `#line` directives for debugger mapping.

## External DSL files offer the cleanest path forward

Razor and SpecFlow/Reqnroll demonstrate proven patterns for file-to-class transformation. Razor switched from MSBuild tasks to source generators in .NET 6, gaining single-pass compilation and incremental builds. Reqnroll (the community fork succeeding SpecFlow in 2024) still uses MSBuild task-based generation but has active discussion about migrating to source generators.

The recommended architecture for a `.spec` or `.draft` testing DSL:

| Component   | Technology                                           | Purpose                                 |
| ----------- | ---------------------------------------------------- | --------------------------------------- |
| File format | Custom grammar (ANTLR or hand-written parser)        | Define test structure                   |
| Generator   | `IIncrementalGenerator`                              | Transform files during compilation      |
| Integration | AdditionalFiles + NuGet package with .props/.targets | Automatic project setup                 |
| Debugging   | `#line` directives                                   | Map generated code back to source files |

A spec file like:

```yaml
# LoginTests.spec
feature: Authentication
  scenario: Valid user can login
    given: a valid user exists
    when: they login with correct credentials
    then: login succeeds with session token
```

Generates:

```csharp
// LoginTests.spec.g.cs
[Fact]
[Trait("Feature", "Authentication")]
#line 3 "LoginTests.spec"
public async Task Valid_user_can_login()
{
    await Given_a_valid_user_exists();
    var result = await When_they_login_with_correct_credentials();
    Assert.True(result.Success);
}
```

This approach delivers **type safety**, **IDE integration**, and **compile-time error detection**—advantages JavaScript testing frameworks sacrifice for runtime flexibility.

## C# scripting enables fluent DSL syntax with trade-offs

dotnet-script 2.0.0 (November 2025) supports .NET 10 with multi-file capability through `#load` directives. This enables actual describe/it syntax:

```csharp
// tests.csx
#load "nuget:ScriptUnit, 0.1.0"
#r "nuget:FluentAssertions, 4.19.4"

public class CalculatorTests {
    public void Should_add_numbers() => (1 + 1).Should().Be(2);
}

return await AddTestsFrom<CalculatorTests>().Execute();
```

Performance equals compiled code after initial JIT, with execution caching preventing recompilation. However, significant trade-offs exist:

- **No direct xUnit/NUnit/MSTest integration**—requires custom runners like ScriptUnit
- **OmniSharp required**—C# Dev Kit doesn't support .csx files
- **Limited namespace support**—all `#load` files share global scope
- **Tooling maturity**—debugging works but is less polished than project-based development

Microsoft's file-based apps (`dotnet run app.cs`) provide an official on-ramp with project conversion capability (`dotnet project convert app.cs`), but remain single-file only until .NET 11.

## JavaScript patterns don't translate directly to .NET's compilation model

Jest, Mocha, and Vitest achieve their ceremony-free syntax through **runtime interpretation**—no code generation occurs. The `describe()` and `it()` functions are simply runtime registrations that build a test tree during execution:

```javascript
// Jest discovers by convention: **/*.test.js
test("adds numbers", () => expect(add(1, 2)).toBe(3));
```

This pattern _could_ work in .NET via `Microsoft.CodeAnalysis.CSharp.Scripting` embedded in a test project—evaluating DSL expressions at runtime. But you lose:

- **Compile-time type checking** on test code
- **IDE navigation** to test definitions
- **Refactoring support** for test method names
- **Static analysis** and code coverage accuracy

The trade-off favors different approaches: JavaScript's dynamic nature makes runtime interpretation natural, while .NET's static typing makes compile-time generation more valuable.

## Most viable implementation paths ranked by practicality

**Path 1: Custom file format + source generator** (Recommended)
Create `.spec` or `.draft` files with a custom DSL, processed by an `IIncrementalGenerator`. Package as NuGet with automatic MSBuild integration. This mirrors Razor's architecture and provides full IDE support, incremental compilation, and debugger mapping via `#line` directives.

**Path 2: YAML/Markdown specs + source generator**
Lower barrier—use existing formats. A generator reads `tests/*.spec.yaml` and emits test classes. Less expressive than custom syntax but zero parser development.

**Path 3: C# scripting with runtime DSL**
Use `Microsoft.CodeAnalysis.CSharp.Scripting` to evaluate test definitions at runtime. Enables natural C# syntax but sacrifices compile-time checking. Works well for expression-based assertions embedded in otherwise traditional tests.

**Path 4: Hybrid approach**
Standard xUnit project with source generator reading external specs. User-defined setup/teardown in partial classes, generated test methods from spec files. Combines extensibility with DSL simplicity.

## Conclusion

.NET 10/C# 14 doesn't unlock the JavaScript-style ceremony-free testing syntax directly in C# files—source generators' additive-only constraint prevents transforming inline DSL code. The **external file + source generator** pattern remains the most robust path, delivering type safety and IDE integration while enabling custom syntax. For teams wanting describe/it syntax specifically, C# scripting with custom runners provides a workable solution, though with tooling trade-offs. The fundamental tension between .NET's static compilation model and dynamic DSL syntax means some ceremony is unavoidable—but the ceremony can live in build infrastructure rather than test files.
