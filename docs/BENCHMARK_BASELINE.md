# DraftSpec Performance Benchmarks - Baseline

**Date**: 2025-12-15
**System**: Apple M3 Max, macOS 26.2
**Runtime**: .NET 10.0.0, Arm64 RyuJIT AdvSIMD

## Summary

| Component | Small | Medium | Large | Scaling |
|-----------|-------|--------|-------|---------|
| SpecRunner | 2.6µs (10 specs) | 24µs (100 specs) | 239µs (1000 specs) | Linear |
| ReportBuilder | 1.2µs (10 specs) | 12.4µs (100 specs) | 118.7µs (1000 specs) | Linear |
| Console Formatter | 1.1µs | - | 44µs | ~41x |
| HTML Formatter | 0.7µs | - | 12.2µs | ~17x |
| Markdown Formatter | 0.3µs | - | 7.3µs | ~22x |

## Detailed Results

### SpecRunner Benchmarks

| Method | Mean | Allocated |
|--------|------|-----------|
| SmallTree_10Specs | 2.606 µs | 15,432 B |
| MediumTree_100Specs | 23.965 µs | 149,024 B |
| LargeTree_1000Specs | 238.745 µs | 1,467,952 B |
| DeepTree_50Levels | 18.597 µs | 111,016 B |
| LargeTree_Parallel (2) | 353.232 µs | 1,524,310 B |
| LargeTree_Parallel (4) | 923.505 µs | 1,599,390 B |

**Note**: Sync-only benchmarks show parallelism overhead. See async benchmarks below for real speedup.

### Async Parallelism (20 specs × 10ms delay)

| Method | Mean | Speedup | Allocated |
|--------|------|---------|-----------|
| AsyncSpecs_Sequential | 217.87 ms | 1.0x | 38 KB |
| AsyncSpecs_Parallel (2) | 109.02 ms | **2.0x** | 41.5 KB |
| AsyncSpecs_Parallel (4) | 54.72 ms | **4.0x** | 41.6 KB |
| AsyncSpecs_Parallel (8) | 32.92 ms | **6.6x** | 42.1 KB |

**Key Finding**: Near-linear scaling with async workloads. Theoretical minimum is 25ms (20÷8×10ms); actual 32.9ms shows ~24% overhead for coordination.

### ReportBuilder Benchmarks

| Method | Mean | Allocated |
|--------|------|-----------|
| Small_10Specs | 1.215 µs | 3,768 B |
| Medium_100Specs | 12.378 µs | 28,402 B |
| Large_1000Specs | 118.654 µs | 238,845 B |

### Formatter Benchmarks

| Method | Mean | Allocated | Ratio |
|--------|------|-----------|-------|
| Console_Small | 1.067 µs | 3,184 B | 1.00 |
| Console_Large | 43.996 µs | 107,768 B | 41.22 |
| Html_Small | 720.864 ns | 7,568 B | 1.00 |
| Html_Large | 12.212 µs | 119,960 B | 16.94 |
| Markdown_Small | 331.950 ns | 2,200 B | 1.00 |
| Markdown_Large | 7.272 µs | 57,392 B | 21.91 |

### Expectation Benchmarks

| Method | Mean | Allocated |
|--------|------|-----------|
| ToBe_Int | ~0 ns | 0 B |
| ToBe_String | ~0 ns | 0 B |
| String_ToContain | 7.566 ns | 0 B |
| String_ToStartWith | 6.978 ns | 0 B |
| Collection_ToContain_Array | 47.862 ns | 32 B |
| Collection_ToContain_List | 49.710 ns | 32 B |
| Collection_ToHaveCount | 0.804 ns | 0 B |
| Action_ToNotThrow | 3.836 ns | 32 B |
| Action_ToThrow_Success | 2.397 µs | 352 B |
| Comparison_ToBeGreaterThan | 2.113 ns | 24 B |
| Comparison_ToBeInRange | ~0 ns | 0 B |

**Key Achievement**: Most passing assertions achieve near-zero overhead and zero allocations.

## Running Benchmarks

```bash
# Run all benchmarks
dotnet run --project benchmarks/DraftSpec.Benchmarks -c Release -- --filter '*'

# Run specific category
dotnet run --project benchmarks/DraftSpec.Benchmarks -c Release -- --filter '*SpecRunner*'
dotnet run --project benchmarks/DraftSpec.Benchmarks -c Release -- --filter '*Expectation*'
```

## Interpretation

1. **Linear Scaling**: SpecRunner and ReportBuilder scale linearly with spec count (10x specs = ~10x time)
2. **Memory Efficiency**: Allocations scale proportionally with spec count
3. **Zero-Overhead Assertions**: Simple equality checks are optimized away by JIT
4. **Fast Formatters**: Markdown is fastest, followed by HTML, then Console
5. **Parallelism**: Near-linear speedup for async workloads; sync-only specs see coordination overhead
