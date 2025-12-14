# Code Quality Review

## Summary

DraftSpec demonstrates good adherence to Clean Code principles with well-structured domain models and clear separation of concerns. However, several areas need refactoring to improve maintainability and reduce complexity.

## Critical Issues

### 1. God Object: Dsl.cs (460 lines)

**Location:** `src/DraftSpec/Dsl.cs`

**Problem:** Single class handling 6+ responsibilities:
- DSL methods (describe, it, fit, xit)
- Context management
- Expectation creation
- Spec execution
- Console output formatting
- JSON output serialization

**Recommendation:** Split into focused classes:

```
src/DraftSpec/
├── Dsl.cs                     # Context management ONLY
├── Expectations/              # Already well-separated
├── Execution/
│   └── SpecRunner.cs          # Already exists
├── Reporting/
│   ├── IReporter.cs
│   ├── ConsoleReporter.cs
│   └── JsonReporter.cs
└── Formatting/
    ├── ResultFormatter.cs
    └── DurationFormatter.cs
```

### 2. String Replacement Fragility

**Location:** `src/DraftSpec.Cli/SpecFileRunner.cs:138-140`

**Problem:** Naive string replacement for script modification:
```csharp
var modifiedScript = scriptContent
    .Replace("run();", "run(json: true);")
    .Replace("run()", "run(json: true)");
```

**Issues:**
- Could match `run()` in comments, strings, or other contexts
- No validation that replacement succeeded
- Could replace partial matches incorrectly

**Recommendation:** Use regex with word boundaries:
```csharp
var modifiedScript = Regex.Replace(
    scriptContent,
    @"\brun\s*\(\s*\)\s*;",
    "run(json: true);",
    RegexOptions.Multiline);
```

### 3. Missing Validation in SpecContext

**Location:** `src/DraftSpec/SpecContext.cs:18-23`

**Problem:** No validation for empty/null descriptions:
```csharp
public SpecContext(string description, SpecContext? parent = null)
{
    Description = description;  // Could be null or empty
    Parent = parent;
    parent?.Children.Add(this);
}
```

**Recommendation:**
```csharp
public SpecContext(string description, SpecContext? parent = null)
{
    if (string.IsNullOrWhiteSpace(description))
        throw new ArgumentException("Description cannot be empty", nameof(description));

    Description = description;
    Parent = parent;
    parent?.Children.Add(this);
}
```

## Code Smells

### 4. Duplicate Format Methods

**Locations:**
- `src/DraftSpec/Expectations/Expectation.cs:162-170`
- `src/DraftSpec/Expectations/CollectionExpectation.cs:106-114`

**Problem:** Identical `Format` method duplicated across classes.

**Recommendation:** Extract to shared utility:
```csharp
internal static class ExpectationHelpers
{
    public static string Format(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        _ => value.ToString() ?? "null"
    };
}
```

### 5. Magic Strings for Output Formats

**Location:** `src/DraftSpec.Cli/Commands/RunCommand.cs:15-16, 70-86`

**Problem:** Format strings scattered throughout:
```csharp
var needsJson = options.Format is "json" or "markdown" or "html";
```

**Recommendation:** Use constants or enum:
```csharp
public static class OutputFormats
{
    public const string Console = "console";
    public const string Json = "json";
    public const string Markdown = "markdown";
    public const string Html = "html";

    public static bool RequiresJson(string format) =>
        format is Json or Markdown or Html;
}
```

### 6. Property Setters as Hooks

**Location:** `src/DraftSpec/Spec.cs:67-70`

**Problem:** Using property setters for side effects is confusing:
```csharp
protected Action beforeAll { set => CurrentContext!.BeforeAll = value; }
```

**Recommendation:** Use explicit methods:
```csharp
protected void beforeAll(Action hook) => CurrentContext!.BeforeAll = hook;
```

### 7. Mutable Public Collections

**Location:** `src/DraftSpec/SpecContext.cs:10-11`

**Problem:** Callers can modify collections directly:
```csharp
public List<SpecContext> Children { get; } = [];
public List<SpecDefinition> Specs { get; } = [];
```

**Recommendation:** Use read-only collections:
```csharp
private readonly List<SpecContext> _children = [];
private readonly List<SpecDefinition> _specs = [];

public IReadOnlyList<SpecContext> Children => _children;
public IReadOnlyList<SpecDefinition> Specs => _specs;
```

## Design Issues

### 8. No Abstraction for SpecRunner

**Location:** `src/DraftSpec/SpecRunner.cs`

**Problem:** Concrete class with no interface makes testing and extension difficult.

**Recommendation:**
```csharp
public interface ISpecRunner
{
    List<SpecResult> Run(SpecContext rootContext);
}
```

### 9. Tight Coupling to ProcessHelper

**Location:** `src/DraftSpec.Cli/SpecFileRunner.cs`

**Problem:** Direct static calls make testing difficult.

**Recommendation:** Inject dependency:
```csharp
public interface IProcessRunner
{
    ProcessResult RunDotnet(string args, string workingDir);
}

public class SpecFileRunner
{
    private readonly IProcessRunner _processRunner;

    public SpecFileRunner(IProcessRunner? processRunner = null)
    {
        _processRunner = processRunner ?? new ProcessHelper();
    }
}
```

### 10. Missing Async Support

**Problem:** No support for async specs:
```csharp
// Not currently possible:
it("async operation", async () =>
{
    var result = await someAsyncMethod();
    expect(result).toBe(expected);
});
```

**Recommendation:** Add `Func<Task>` overloads:
```csharp
public static void it(string description, Func<Task> body)
{
    EnsureContext();
    CurrentContext!.AddSpec(new AsyncSpecDefinition(description, body));
}
```

## Naming Conventions

### 11. camelCase for Public Methods

**Files:** All DSL methods (describe, it, expect)

**Note:** Intentional to match RSpec/Jest conventions, but breaks .NET guidelines.

**Recommendation:** Document this design decision. Consider PascalCase aliases:
```csharp
public static void Describe(string description, Action body) => describe(description, body);
```

### 12. Inconsistent Hook Naming

**Files:**
- `SpecContext.cs:15` - BeforeEach
- `Dsl.cs:122` - before

**Recommendation:** Align naming or document the intentional difference.

## Documentation Gaps

### 13. Missing XML Documentation

**Problem:** Methods lack parameter descriptions, return documentation, exceptions, and examples.

**Example improvement:**
```csharp
/// <summary>
/// Recursively executes all specs in a context and its children.
/// </summary>
/// <param name="context">The context to execute</param>
/// <param name="parentDescriptions">Accumulated parent descriptions for path building</param>
/// <param name="results">List to accumulate results into</param>
/// <param name="hasFocused">Whether any focused specs exist in the tree</param>
private void RunContext(...)
```

## Priority Summary

| Priority | Issue | Effort |
|----------|-------|--------|
| P0 | Split Dsl.cs | High |
| P0 | Fix string replacement | Low |
| P1 | Add validation | Low |
| P1 | Eliminate duplicate code | Low |
| P1 | Make collections immutable | Medium |
| P2 | Add abstractions | Medium |
| P2 | Add async support | High |
| P2 | Improve documentation | Medium |
