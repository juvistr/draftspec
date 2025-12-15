using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace DraftSpec.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for expectation assertion performance.
/// Goal: Zero allocations for passing assertions.
/// </summary>
[MemoryDiagnoser]
public class ExpectationBenchmarks
{
    private const int N = 1000;

    private int[] _intArray = null!;
    private List<int> _intList = null!;

    [GlobalSetup]
    public void Setup()
    {
        _intArray = Enumerable.Range(0, N).ToArray();
        _intList = Enumerable.Range(0, N).ToList();
    }

    // Basic equality
    [Benchmark]
    public void ToBe_Int()
    {
        var exp = new Expectation<int>(42, "value");
        exp.toBe(42);
    }

    [Benchmark]
    public void ToBe_String()
    {
        var exp = new Expectation<string>("hello world", "value");
        exp.toBe("hello world");
    }

    // String assertions
    [Benchmark]
    public void String_ToContain()
    {
        var exp = new StringExpectation("hello world", "value");
        exp.toContain("world");
    }

    [Benchmark]
    public void String_ToStartWith()
    {
        var exp = new StringExpectation("hello world", "value");
        exp.toStartWith("hello");
    }

    // Collection assertions
    [Benchmark]
    public void Collection_ToContain_Array()
    {
        var exp = new CollectionExpectation<int>(_intArray, "items");
        exp.toContain(500);
    }

    [Benchmark]
    public void Collection_ToContain_List()
    {
        var exp = new CollectionExpectation<int>(_intList, "items");
        exp.toContain(500);
    }

    [Benchmark]
    public void Collection_ToHaveCount()
    {
        var exp = new CollectionExpectation<int>(_intArray, "items");
        exp.toHaveCount(N);
    }

    // Exception handling (common path)
    [Benchmark]
    public void Action_ToNotThrow()
    {
        var exp = new ActionExpectation(() => { }, "action");
        exp.toNotThrow();
    }

    [Benchmark]
    public InvalidOperationException Action_ToThrow_Success()
    {
        var exp = new ActionExpectation(
            () => throw new InvalidOperationException("test"),
            "action");
        return exp.toThrow<InvalidOperationException>();
    }

    // Comparison operators
    [Benchmark]
    public void Comparison_ToBeGreaterThan()
    {
        var exp = new Expectation<int>(100, "value");
        exp.toBeGreaterThan(50);
    }

    [Benchmark]
    public void Comparison_ToBeInRange()
    {
        var exp = new Expectation<int>(50, "value");
        exp.toBeInRange(0, 100);
    }
}
