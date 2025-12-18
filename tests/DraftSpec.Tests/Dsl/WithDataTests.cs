using static DraftSpec.Dsl;

namespace DraftSpec.Tests.Dsl;

/// <summary>
/// Tests for table-driven test support (withData).
/// </summary>
public class WithDataTests
{
    [Before(Test)]
    public void SetUp()
    {
        // Reset DSL state before each test
        Reset();
    }

    #region Basic withData<T>

    [Test]
    public async Task WithData_SingleType_GeneratesSpecsForEachItem()
    {
        var executed = new List<int>();

        describe("numbers", () =>
        {
            withData([1, 2, 3, 4, 5], n =>
            {
                it($"processes {n}", () => executed.Add(n));
            });
        });

        run();

        await Assert.That(executed).Count().IsEqualTo(5);
        await Assert.That(executed).Contains(1);
        await Assert.That(executed).Contains(5);
    }

    [Test]
    public async Task WithData_AnonymousObjects_WorksCorrectly()
    {
        var results = new List<string>();

        describe("strings", () =>
        {
            withData([
                new { input = "hello", expected = 5 },
                new { input = "world", expected = 5 },
                new { input = "", expected = 0 }
            ], data =>
            {
                it($"'{data.input}' has length {data.expected}", () =>
                {
                    results.Add($"{data.input}:{data.input.Length}");
                    if (data.input.Length != data.expected)
                        throw new Exception("Length mismatch");
                });
            });
        });

        run();

        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results).Contains("hello:5");
        await Assert.That(results).Contains(":0");
    }

    #endregion

    #region Tuple Overloads

    [Test]
    public async Task WithData_Tuple2_DestructuresCorrectly()
    {
        var results = new List<string>();

        describe("pairs", () =>
        {
            withData([
                ("a", 1),
                ("b", 2),
                ("c", 3)
            ], (letter, number) =>
            {
                it($"{letter} = {number}", () => results.Add($"{letter}{number}"));
            });
        });

        run();

        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results).Contains("a1");
        await Assert.That(results).Contains("c3");
    }

    [Test]
    public async Task WithData_Tuple3_AddsCorrectly()
    {
        var results = new List<(int a, int b, int sum)>();

        describe("Calculator", () =>
        {
            withData([
                (1, 1, 2),
                (2, 3, 5),
                (0, 0, 0),
                (-1, 1, 0)
            ], (a, b, expected) =>
            {
                it($"adds {a} + {b} = {expected}", () =>
                {
                    var actual = a + b;
                    results.Add((a, b, actual));
                    if (actual != expected)
                        throw new Exception($"Expected {expected} but got {actual}");
                });
            });
        });

        run();

        await Assert.That(results).Count().IsEqualTo(4);
        await Assert.That(results.All(r => r.a + r.b == r.sum)).IsTrue();
    }

    [Test]
    public async Task WithData_Tuple4_WorksCorrectly()
    {
        var count = 0;

        describe("4-tuples", () =>
        {
            withData([
                (1, 2, 3, 6),
                (2, 3, 4, 9)
            ], (a, b, c, sum) =>
            {
                it($"{a}+{b}+{c}={sum}", () =>
                {
                    count++;
                    if (a + b + c != sum) throw new Exception("Sum mismatch");
                });
            });
        });

        run();

        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async Task WithData_Tuple5_WorksCorrectly()
    {
        var count = 0;

        describe("5-tuples", () =>
        {
            withData([
                (1, 2, 3, 4, 10),
                (0, 0, 0, 0, 0)
            ], (a, b, c, d, sum) =>
            {
                it($"sums to {sum}", () =>
                {
                    count++;
                    if (a + b + c + d != sum) throw new Exception("Sum mismatch");
                });
            });
        });

        run();

        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async Task WithData_Tuple6_WorksCorrectly()
    {
        var count = 0;

        describe("6-tuples", () =>
        {
            withData([
                (1, 2, 3, 4, 5, 15),
                (0, 0, 0, 0, 0, 0)
            ], (a, b, c, d, e, sum) =>
            {
                it($"sums to {sum}", () =>
                {
                    count++;
                    if (a + b + c + d + e != sum) throw new Exception("Sum mismatch");
                });
            });
        });

        run();

        await Assert.That(count).IsEqualTo(2);
    }

    #endregion

    #region Dictionary Overload

    [Test]
    public async Task WithData_Dictionary_UsesKeysAsNames()
    {
        var specNames = new List<string>();

        describe("named cases", () =>
        {
            withData(new Dictionary<string, (int, int, int)>
            {
                ["positive numbers"] = (1, 2, 3),
                ["with zero"] = (0, 5, 5),
                ["negative result"] = (1, -5, -4)
            }, (name, data) =>
            {
                it(name, () =>
                {
                    specNames.Add(name);
                    var (a, b, expected) = data;
                    if (a + b != expected) throw new Exception("Mismatch");
                });
            });
        });

        run();

        await Assert.That(specNames).Count().IsEqualTo(3);
        await Assert.That(specNames).Contains("positive numbers");
        await Assert.That(specNames).Contains("with zero");
        await Assert.That(specNames).Contains("negative result");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task WithData_EmptyCollection_GeneratesNoSpecs()
    {
        var executed = false;

        describe("empty", () =>
        {
            withData(Array.Empty<int>(), n =>
            {
                it($"spec {n}", () => executed = true);
            });
        });

        run();

        await Assert.That(executed).IsFalse();
    }

    [Test]
    public async Task WithData_NestedInContext_WorksCorrectly()
    {
        var results = new List<string>();

        describe("outer", () =>
        {
            describe("inner", () =>
            {
                withData(["a", "b"], item =>
                {
                    it($"item {item}", () => results.Add(item));
                });
            });
        });

        run();

        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results).Contains("a");
        await Assert.That(results).Contains("b");
    }

    [Test]
    public async Task WithData_MultipleCallsInSameContext_Accumulate()
    {
        var count = 0;

        describe("multiple withData", () =>
        {
            withData([1, 2], n =>
            {
                it($"first batch {n}", () => count++);
            });

            withData([3, 4, 5], n =>
            {
                it($"second batch {n}", () => count++);
            });
        });

        run();

        await Assert.That(count).IsEqualTo(5);
    }

    [Test]
    public async Task WithData_WithFailingSpec_ReportsCorrectly()
    {
        describe("failures", () =>
        {
            withData([
                (1, 1, 2),   // pass
                (1, 1, 3),   // fail - wrong expected
                (2, 2, 4)    // pass
            ], (a, b, expected) =>
            {
                it($"{a}+{b}={expected}", () =>
                {
                    if (a + b != expected)
                        throw new Exception($"Expected {expected} but got {a + b}");
                });
            });
        });

        var results = new SpecRunner().Run(RootContext!);

        await Assert.That(results.Count(r => r.Status == SpecStatus.Passed)).IsEqualTo(2);
        await Assert.That(results.Count(r => r.Status == SpecStatus.Failed)).IsEqualTo(1);
    }

    #endregion

    #region Real-World Scenarios

    [Test]
    public async Task WithData_StringValidation_RealWorldExample()
    {
        describe("Email validation", () =>
        {
            withData([
                ("user@example.com", true),
                ("invalid-email", false),
                ("user@domain.org", true),
                ("@missing-local.com", false),
                ("missing-at.com", false)
            ], (email, isValid) =>
            {
                var expectedDesc = isValid ? "valid" : "invalid";
                it($"'{email}' is {expectedDesc}", () =>
                {
                    var actualValid = email.Contains('@') && email.IndexOf('@') > 0;
                    if (actualValid != isValid)
                        throw new Exception($"Expected {isValid} but got {actualValid}");
                });
            });
        });

        var results = new SpecRunner().Run(RootContext!);

        await Assert.That(results).Count().IsEqualTo(5);
        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();
    }

    [Test]
    public async Task WithData_BoundaryConditions_RealWorldExample()
    {
        describe("Array bounds checking", () =>
        {
            withData([
                ("empty array", Array.Empty<int>(), -1, false),
                ("single element at 0", new[] { 1 }, 0, true),
                ("single element at 1", new[] { 1 }, 1, false),
                ("three elements at 2", new[] { 1, 2, 3 }, 2, true),
                ("negative index", new[] { 1, 2 }, -1, false)
            ], (name, array, index, shouldBeValid) =>
            {
                it(name, () =>
                {
                    var isValid = index >= 0 && index < array.Length;
                    if (isValid != shouldBeValid)
                        throw new Exception($"Expected {shouldBeValid} but got {isValid}");
                });
            });
        });

        var results = new SpecRunner().Run(RootContext!);

        await Assert.That(results).Count().IsEqualTo(5);
        await Assert.That(results.All(r => r.Status == SpecStatus.Passed)).IsTrue();
    }

    #endregion
}
