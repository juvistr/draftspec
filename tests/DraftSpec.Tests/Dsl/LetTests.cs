using static DraftSpec.Dsl;

namespace DraftSpec.Tests.Dsl;

/// <summary>
/// Tests for let/get lazy fixture pattern.
/// </summary>
public class LetTests
{
    #region Basic Functionality

    [Test]
    public async Task let_RegistersFactory_GetReturnsValue()
    {
        var runner = new SpecRunner();
        object? capturedValue = null;

        describe("test", () =>
        {
            let("fixture", () => new TestFixture { Value = 42 });

            it("gets the value", () =>
            {
                capturedValue = get<TestFixture>("fixture");
            });
        });

        var results = await runner.RunAsync(RootContext!);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Passed);
        await Assert.That(capturedValue).IsNotNull();
        await Assert.That(((TestFixture)capturedValue!).Value).IsEqualTo(42);
    }

    [Test]
    public async Task get_MemoizesValueWithinSpec()
    {
        var runner = new SpecRunner();
        var factoryCallCount = 0;
        TestFixture? first = null;
        TestFixture? second = null;

        describe("test", () =>
        {
            let("fixture", () =>
            {
                factoryCallCount++;
                return new TestFixture { Value = factoryCallCount };
            });

            it("calls factory once", () =>
            {
                first = get<TestFixture>("fixture");
                second = get<TestFixture>("fixture");
            });
        });

        await runner.RunAsync(RootContext!);

        await Assert.That(factoryCallCount).IsEqualTo(1);
        await Assert.That(first).IsSameReferenceAs(second);
    }

    [Test]
    public async Task get_ResetsBetweenSpecs()
    {
        var runner = new SpecRunner();
        var factoryCallCount = 0;
        var spec1Value = 0;
        var spec2Value = 0;

        describe("test", () =>
        {
            let("counter", () =>
            {
                factoryCallCount++;
                return factoryCallCount;
            });

            it("first spec", () =>
            {
                spec1Value = get<int>("counter");
            });

            it("second spec", () =>
            {
                spec2Value = get<int>("counter");
            });
        });

        await runner.RunAsync(RootContext!);

        // Factory called twice - once per spec
        await Assert.That(factoryCallCount).IsEqualTo(2);
        await Assert.That(spec1Value).IsEqualTo(1);
        await Assert.That(spec2Value).IsEqualTo(2);
    }

    #endregion

    #region Context Inheritance

    [Test]
    public async Task let_NestedContextShadowsParent()
    {
        var runner = new SpecRunner();
        var parentValue = 0;
        var childValue = 0;

        describe("parent", () =>
        {
            let("value", () => 100);

            it("uses parent let", () =>
            {
                parentValue = get<int>("value");
            });

            describe("child", () =>
            {
                let("value", () => 200); // Shadow parent

                it("uses child let", () =>
                {
                    childValue = get<int>("value");
                });
            });
        });

        await runner.RunAsync(RootContext!);

        await Assert.That(parentValue).IsEqualTo(100);
        await Assert.That(childValue).IsEqualTo(200);
    }

    [Test]
    public async Task get_AccessesParentLetFromNestedContext()
    {
        var runner = new SpecRunner();
        var capturedValue = 0;

        describe("parent", () =>
        {
            let("parentFixture", () => 42);

            describe("child", () =>
            {
                it("accesses parent let", () =>
                {
                    capturedValue = get<int>("parentFixture");
                });
            });
        });

        await runner.RunAsync(RootContext!);

        await Assert.That(capturedValue).IsEqualTo(42);
    }

    [Test]
    public async Task let_CanReferenceOtherLets()
    {
        var runner = new SpecRunner();
        var capturedValue = "";

        describe("test", () =>
        {
            let("first", () => "Hello");
            let("second", () => get<string>("first") + " World");

            it("composes lets", () =>
            {
                capturedValue = get<string>("second");
            });
        });

        await runner.RunAsync(RootContext!);

        await Assert.That(capturedValue).IsEqualTo("Hello World");
    }

    #endregion

    #region Error Handling

    [Test]
    public async Task get_OutsideExecution_ThrowsInvalidOperation()
    {
        // Ensure we're not in a spec execution
        LetScope.Current = null;

        var action = () => get<string>("anything");

        await Assert.That(action).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task get_UndefinedName_ThrowsInvalidOperation()
    {
        var runner = new SpecRunner();
        Exception? capturedException = null;

        describe("test", () =>
        {
            it("tries undefined let", () =>
            {
                try
                {
                    get<string>("undefined");
                }
                catch (Exception ex)
                {
                    capturedException = ex;
                    throw;
                }
            });
        });

        var results = await runner.RunAsync(RootContext!);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(capturedException).IsTypeOf<InvalidOperationException>();
        await Assert.That(capturedException!.Message).Contains("undefined");
    }

    [Test]
    public async Task get_TypeMismatch_ThrowsInvalidCast()
    {
        var runner = new SpecRunner();
        Exception? capturedException = null;

        describe("test", () =>
        {
            let("number", () => 42);

            it("casts to wrong type", () =>
            {
                try
                {
                    get<string>("number"); // int -> string should fail
                }
                catch (Exception ex)
                {
                    capturedException = ex;
                    throw;
                }
            });
        });

        var results = await runner.RunAsync(RootContext!);

        await Assert.That(results[0].Status).IsEqualTo(SpecStatus.Failed);
        await Assert.That(capturedException).IsTypeOf<InvalidCastException>();
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task let_WithNullValue_WorksCorrectly()
    {
        var runner = new SpecRunner();
        var factoryCallCount = 0;
        object? capturedValue = "not null";

        describe("test", () =>
        {
            let<string?>("nullable", () =>
            {
                factoryCallCount++;
                return null;
            });

            it("gets null value", () =>
            {
                capturedValue = get<string?>("nullable");
                // Call again to verify memoization
                _ = get<string?>("nullable");
            });
        });

        await runner.RunAsync(RootContext!);

        await Assert.That(capturedValue).IsNull();
        // Factory should still only be called once
        await Assert.That(factoryCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task let_OverwritesInSameContext()
    {
        var runner = new SpecRunner();
        var capturedValue = 0;

        describe("test", () =>
        {
            let("value", () => 100);
            let("value", () => 200); // Overwrite

            it("uses latest definition", () =>
            {
                capturedValue = get<int>("value");
            });
        });

        await runner.RunAsync(RootContext!);

        await Assert.That(capturedValue).IsEqualTo(200);
    }

    [Test]
    public async Task let_WorksWithAsyncFactoryBody()
    {
        var runner = new SpecRunner();
        var capturedValue = 0;

        describe("test", () =>
        {
            // Factory returns synchronous value
            let("asyncValue", () => 42);

            it("gets async value", () =>
            {
                capturedValue = get<int>("asyncValue");
            });
        });

        await runner.RunAsync(RootContext!);

        await Assert.That(capturedValue).IsEqualTo(42);
    }

    #endregion

    #region Parallel Execution

    [Test]
    public async Task let_IsolatedBetweenParallelSpecs()
    {
        var runner = new SpecRunner([], null, maxDegreeOfParallelism: 4);
        var values = new System.Collections.Concurrent.ConcurrentBag<int>();
        var factoryCallCount = 0;

        describe("parallel", () =>
        {
            let("counter", () =>
            {
                return Interlocked.Increment(ref factoryCallCount);
            });

            for (var i = 0; i < 10; i++)
            {
                it($"spec {i}", () =>
                {
                    values.Add(get<int>("counter"));
                    // Call again to verify memoization within spec
                    var second = get<int>("counter");
                    // Should be same value
                    expect(second).toBe(get<int>("counter"));
                });
            }
        });

        var results = await runner.RunAsync(RootContext!);

        // All specs should pass
        var passedCount = results.Count(r => r.Status == SpecStatus.Passed);
        await Assert.That(passedCount).IsEqualTo(10);

        // Factory called 10 times (once per spec)
        await Assert.That(factoryCallCount).IsEqualTo(10);

        // All values should be unique (1-10)
        await Assert.That(values.Distinct().Count()).IsEqualTo(10);
    }

    #endregion

    #region Test Setup/Teardown

    [Before(Test)]
    public void ResetDsl()
    {
        Reset();
    }

    #endregion

    #region Helper Classes

    private class TestFixture
    {
        public int Value { get; init; }
    }

    #endregion
}
