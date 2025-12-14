# Performance Analysis

## Summary

DraftSpec is lightweight and efficient for small to medium test suites (<1,000 specs). However, several bottlenecks emerge at scale that require optimization for large suites.

## Scaling Profile

| Suite Size | Specs | Current Overhead | Target |
|------------|-------|------------------|--------|
| Small | 1-100 | <10ms | OK |
| Medium | 100-1,000 | 50-100ms | OK |
| Large | 1,000-10,000 | 500ms-2s | Needs work |
| Very Large | 10,000+ | 5-20s | Needs optimization |

## Critical Bottlenecks

### 1. O(n²) JSON Tree Building Algorithm

**Location:** `src/DraftSpec/Dsl.cs:383-425`
**Impact:** CRITICAL for large suites with JSON output

**Problem:** Linear search for each spec:
```csharp
foreach (var spec in context.Specs)
{
    var result = allResults.FirstOrDefault(r =>
        r.Spec == spec && r.ContextPath.SequenceEqual(currentPath));
    // ...
}
```

**Cost:** 10,000 specs = ~25 million operations

**Fix:**
```csharp
// Build lookup dictionary first
var resultLookup = allResults.ToDictionary(
    r => (r.Spec, string.Join("/", r.ContextPath)),
    r => r
);

// O(1) lookup per spec
var key = (spec, string.Join("/", currentPath));
if (resultLookup.TryGetValue(key, out var result))
{
    // Use result
}
```

**Expected improvement:** 100-1000x for JSON output

### 2. Hook Chain Reconstruction Per Spec

**Location:** `src/DraftSpec/SpecRunner.cs:110-125`
**Impact:** HIGH - overhead proportional to spec count × nesting depth

**Problem:** Stack allocation + tree walk for every spec:
```csharp
private static void RunBeforeEachHooks(SpecContext context)
{
    var contexts = new Stack<SpecContext>();  // Allocation per spec
    var current = context;
    while (current != null)
    {
        contexts.Push(current);
        current = current.Parent;
    }
    // ...
}
```

**Cost:** 1,000 specs × 5 levels = 5,000 allocations + 5,000 tree walks

**Fix:** Cache hook chains during tree construction:
```csharp
// In SpecContext
private List<Action>? _beforeEachChain;

public List<Action> GetBeforeEachChain()
{
    if (_beforeEachChain == null)
    {
        _beforeEachChain = [];
        var current = this;
        while (current != null)
        {
            if (current.BeforeEach != null)
                _beforeEachChain.Insert(0, current.BeforeEach);
            current = current.Parent;
        }
    }
    return _beforeEachChain;
}
```

**Expected improvement:** 5-10x reduction in hook overhead

### 3. Focus Detection Full Tree Scan

**Location:** `src/DraftSpec/SpecRunner.cs:22-28`
**Impact:** MEDIUM - unnecessary work when focused specs exist

**Problem:** Continues scanning after finding focused spec:
```csharp
private static bool HasFocusedSpecs(SpecContext context)
{
    if (context.Specs.Any(s => s.IsFocused))
        return true;
    return context.Children.Any(HasFocusedSpecs);  // No early exit
}
```

**Fix:** Explicit early exit:
```csharp
private static bool HasFocusedSpecs(SpecContext context)
{
    if (context.Specs.Any(s => s.IsFocused))
        return true;

    foreach (var child in context.Children)
    {
        if (HasFocusedSpecs(child))
            return true;  // Early exit
    }
    return false;
}
```

**Expected improvement:** 2-100x when focused specs exist

### 4. Console Output String Allocations

**Location:** `src/DraftSpec/Dsl.cs:268-283`
**Impact:** MEDIUM - affects developer experience

**Problem:** string.Join + Take on every path segment:
```csharp
for (int i = 0; i < result.ContextPath.Count; i++)
{
    var pathKey = string.Join("/", result.ContextPath.Take(i + 1));
    // ...
}
```

**Cost:** 1,000 specs × 5 levels = 5,000+ allocations

**Fix:** Build path incrementally:
```csharp
var currentPath = new StringBuilder();
for (int i = 0; i < result.ContextPath.Count; i++)
{
    if (i > 0) currentPath.Append('/');
    currentPath.Append(result.ContextPath[i]);
    var pathKey = currentPath.ToString();
    // ...
}
```

**Expected improvement:** 2-5x console output speed

### 5. Context Path Copying

**Location:** `src/DraftSpec/SpecRunner.cs:36`
**Impact:** LOW-MEDIUM - accumulates with deep nesting

**Problem:** List copied on every context recursion:
```csharp
var descriptions = parentDescriptions.ToList();  // Copy per context
```

**Fix:** Pre-compute paths during tree construction (see Architecture doc).

## Quick Wins

### Static JsonSerializerOptions

**Location:** `src/DraftSpec/Dsl.cs:373-378`

**Current:** Options created per run
```csharp
var options = new JsonSerializerOptions { ... };
```

**Fix:**
```csharp
private static readonly JsonSerializerOptions JsonOptions = new()
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
```

### Collection Count Optimization

**Location:** `src/DraftSpec/Expectations/CollectionExpectation.cs`

**Problem:** Count() may enumerate entire collection:
```csharp
var count = _actual.Count();  // O(n) for IEnumerable
```

**Fix:**
```csharp
private int GetCount() => _actual switch
{
    ICollection<T> c => c.Count,
    ICollection c => c.Count,
    _ => _actual.Count()
};
```

## Memory Analysis

### Allocations Per Spec

| Component | Bytes | Notes |
|-----------|-------|-------|
| SpecResult | ~64 | Object + fields |
| Stack<SpecContext> | ~40 | Hook chain |
| List<string> | ~40 | Context path copy |
| Expectation wrappers | ~24 each | Per expect() call |
| **Total typical** | **200-400** | Gen 0 friendly |

### GC Pressure Scaling

| Specs | Allocations | GC Impact |
|-------|-------------|-----------|
| 100 | ~40KB | Negligible |
| 1,000 | ~400KB | Low |
| 10,000 | ~4MB | Moderate |
| 100,000 | ~40MB | High - needs optimization |

## Benchmarking Targets

### Overhead Budget

| Operation | Target |
|-----------|--------|
| Per-spec overhead | <500µs |
| Tree construction | <10ms for 1,000 specs |
| Focus detection | <1ms |
| JSON serialization | <100ms for 1,000 specs |

### Suggested Benchmarks

```csharp
[Test]
public async Task LargeSuite_CompletesWithinBudget()
{
    var spec = CreateLargeSuite(1000);
    var sw = Stopwatch.StartNew();
    var runner = new SpecRunner();
    runner.Run(spec);
    sw.Stop();

    await Assert.That(sw.ElapsedMilliseconds).IsLessThan(500);
}
```

## Optimization Priority

### Phase 1: Quick Wins (1-2 hours)
1. Add early exit to focus detection
2. Make JsonSerializerOptions static
3. Add ICollection checks to collection expectations

### Phase 2: High Impact (4-6 hours)
1. Fix JSON tree building O(n²) algorithm
2. Cache hook chains during construction
3. Pre-compute context paths

### Phase 3: Future (1-2 weeks)
1. Parallel spec execution
2. Performance instrumentation
3. Benchmark test suite

## Concurrency Opportunities

### Current State
- Single-threaded execution
- AsyncLocal for thread-safe DSL
- No parallel spec execution

### Future Opportunities
1. **Parallel spec execution** within independent contexts
2. **Background JSON serialization**
3. **Parallel context execution** when independent

### Challenges
- Hook ordering must remain sequential
- Console output ordering
- Would require opt-in flag
