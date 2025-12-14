# Test Coverage Assessment

## Summary

DraftSpec has **minimal test coverage** (~15-20%) with only 9 test cases across 2 files. For a testing framework expected to test itself, this is insufficient.

## Current Coverage

### Tested (9 tests, 2 files)

**FocusModeTests.cs** (6 tests)
- Single focused spec with `fit()`
- Multiple focused specs
- Focused specs in nested contexts
- All specs run when no focus
- Skipped specs with `xit()`

**HookOrderingTests.cs** (3 tests)
- `beforeEach` parent-to-child order
- `afterEach` child-to-parent order
- `beforeAll` runs once per context

### Not Tested

| Component | Coverage | Gap |
|-----------|----------|-----|
| Expectation API | 0% | All assertions untested |
| DSL (describe/it) | ~20% | Basic usage untested |
| SpecRunner | ~30% | Exception handling untested |
| Output formatting | 0% | Console/JSON untested |
| Edge cases | ~5% | Hook exceptions, deep nesting |
| Integration | 0% | End-to-end untested |

## Critical Gaps

### 1. Expectation API (0% coverage)

**Missing tests for Expectation<T>.cs:**
- toBe() equality
- toBeNull() / toNotBeNull()
- toBeGreaterThan() / toBeLessThan()
- toBeAtLeast() / toBeAtMost()
- toBeInRange()
- toBeCloseTo() with tolerance
- Null handling, type mismatches
- Error message validation

**Missing tests for BoolExpectation.cs:**
- toBeTrue() / toBeFalse()
- toBe() for booleans
- Expression capture

**Missing tests for StringExpectation.cs:**
- toBe() equality
- toContain() substring
- toStartWith() / toEndWith()
- toBeNullOrEmpty()
- Case sensitivity
- Null edge cases

**Missing tests for ActionExpectation.cs:**
- toThrow<TException>()
- toThrow() any exception
- toNotThrow()
- Wrong exception type
- Exception not thrown

**Missing tests for CollectionExpectation.cs:**
- toContain() / toNotContain()
- toContainAll()
- toHaveCount()
- toBeEmpty() / toNotBeEmpty()
- toBe() sequence equality
- Truncation in error messages

### 2. DSL (20% coverage)

**Missing tests:**
- describe() context creation
- Multiple root describes
- context() alias
- it() with/without body
- Nested context handling
- run() console output
- run(json: true) format
- Error when hooks outside describe
- AsyncLocal isolation
- expect() overload resolution

### 3. Core Infrastructure (0% coverage)

**SpecContext.cs:**
- Tree construction
- Parent-child relationships
- AddSpec/AddChild methods
- Hook property setters

**SpecDefinition.cs:**
- Constructor variants
- IsPending logic
- Flag initialization

**SpecResult.cs:**
- Constructor parameters
- FullDescription property

**SpecRunner.cs:**
- Basic execution
- Exception capture
- Duration measurement
- Pending spec handling

### 4. Output (0% coverage)

**Console output:**
- Colored formatting
- Context path printing
- Status symbols
- Duration formatting
- Summary statistics

**JSON output:**
- Structure validation
- Timestamp inclusion
- Error serialization

### 5. Edge Cases (~5% coverage)

**Missing scenarios:**
- Exceptions in hooks
- Exceptions in describe blocks
- Deep nesting (10+ levels)
- Large suites (100+ specs)
- Empty describe blocks
- Unicode in descriptions
- Very long descriptions

## Recommended Test Structure

```
tests/DraftSpec.Tests/
├── Expectations/
│   ├── ExpectationTests.cs          # 15 tests
│   ├── BoolExpectationTests.cs      # 5 tests
│   ├── StringExpectationTests.cs    # 10 tests
│   ├── ActionExpectationTests.cs    # 8 tests
│   └── CollectionExpectationTests.cs # 12 tests
├── Dsl/
│   ├── DslDescribeTests.cs          # 10 tests
│   ├── DslSpecTests.cs              # 8 tests
│   ├── DslHookTests.cs              # 6 tests
│   └── DslExpectTests.cs            # 8 tests
├── Runner/
│   ├── SpecRunnerTests.cs           # 10 tests
│   ├── SpecRunnerHookTests.cs       # (existing)
│   └── SpecRunnerFocusTests.cs      # (existing)
├── Output/
│   ├── ConsoleOutputTests.cs        # 8 tests
│   └── JsonOutputTests.cs           # 8 tests
├── Integration/
│   ├── EndToEndTests.cs             # 6 tests
│   └── RealWorldScenariosTests.cs   # 4 tests
├── EdgeCases/
│   ├── ErrorHandlingTests.cs        # 10 tests
│   ├── PerformanceTests.cs          # 4 tests
│   └── ConcurrencyTests.cs          # 4 tests
└── TestHelpers/
    ├── SpecBuilder.cs
    └── AssertionExtensions.cs
```

**Total new tests needed:** ~120-140

## Priority Order

### Phase 1: Critical (40 tests)
1. **Expectation API** - Core functionality users depend on
2. **Basic Runner** - Execution fundamentals

### Phase 2: Integration (25 tests)
3. **DSL Tests** - describe/it/hooks
4. **Output Tests** - Console/JSON validation

### Phase 3: Quality (55 tests)
5. **Edge Cases** - Error handling, deep nesting
6. **Integration** - End-to-end scenarios
7. **Performance** - Regression prevention

## Coverage Targets

| Metric | Current | Target |
|--------|---------|--------|
| Line Coverage | <20% | 85%+ |
| Branch Coverage | <15% | 80%+ |
| Method Coverage | <25% | 90%+ |

## Test Infrastructure Needs

### Test Helpers

```csharp
public static class SpecBuilder
{
    public static SpecContext CreateContext(string description);
    public static SpecDefinition CreateSpec(string description, Action? body = null);
    public static List<SpecResult> RunSpec(Spec spec);
}

public static class AssertionExtensions
{
    public static void ShouldThrowAssertion(this Action action, string expectedMessage);
    public static void ShouldHaveStatus(this SpecResult result, SpecStatus expected);
}
```

### CI Integration

```bash
# Enable coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate report
reportgenerator -reports:coverage.cobertura.xml -targetdir:coverage

# Fail on threshold
dotnet test /p:CollectCoverage=true /p:Threshold=80
```

## Self-Dogfooding Opportunity

As a testing framework, DraftSpec should dogfood itself:

1. Write some DraftSpec tests using DraftSpec CSX
2. Use specs as living documentation
3. Ensure the framework works for its own use case

## Example Tests Needed

### Expectation Tests

```csharp
[Test]
public async Task toBe_WithEqualValues_Passes()
{
    expect(42).toBe(42);  // Should not throw
}

[Test]
public async Task toBe_WithDifferentValues_ThrowsWithMessage()
{
    var action = () => expect(42).toBe(99);

    await Assert.That(action)
        .Throws<AssertionException>()
        .WithMessage("*to be 99*but was 42*");
}
```

### Runner Tests

```csharp
[Test]
public async Task Run_WithFailingSpec_CapturesException()
{
    var context = new SpecContext("test");
    context.AddSpec(new SpecDefinition("fails", () => throw new Exception("boom")));

    var runner = new SpecRunner();
    var results = runner.Run(context);

    await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
    await Assert.That(results[0].Exception!.Message).IsEqualTo("boom");
}
```

### Output Tests

```csharp
[Test]
public async Task JsonOutput_IncludesAllFields()
{
    // Capture JSON output
    var json = CaptureJsonOutput(() => run(json: true));

    var doc = JsonDocument.Parse(json);
    await Assert.That(doc.RootElement.TryGetProperty("timestamp", out _)).IsTrue();
    await Assert.That(doc.RootElement.TryGetProperty("summary", out _)).IsTrue();
}
```
