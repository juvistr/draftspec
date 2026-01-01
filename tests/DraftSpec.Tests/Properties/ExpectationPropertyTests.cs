using FsCheck;
using FsCheck.Fluent;

namespace DraftSpec.Tests.Properties;

/// <summary>
/// Property-based tests for Expectation<T> matchers using FsCheck.
/// These tests verify mathematical properties that must hold for all inputs.
/// </summary>
public class ExpectationPropertyTests
{
    [Test]
    public void ToBe_IsReflexive()
    {
        // Property: For any value x, x.toBe(x) should always pass
        Prop.ForAll<int>(value =>
        {
            var exp = new Expectation<int>(value, "value");
            exp.toBe(value);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBe_IsReflexive_Strings()
    {
        // Property: For any non-null string, s.toBe(s) should always pass
        Prop.ForAll<NonNull<string>>(s =>
        {
            var exp = new Expectation<string>(s.Get, "value");
            exp.toBe(s.Get);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBeGreaterThan_IsTransitive()
    {
        // Property: If a > b and b > c, then a > c
        Prop.ForAll<int, int, int>((a, b, c) =>
        {
            if (a > b && b > c)
            {
                var exp = new Expectation<int>(a, "a");
                exp.toBeGreaterThan(c);
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBeGreaterThan_IsAntisymmetric()
    {
        // Property: If a > b, then !(b > a)
        Prop.ForAll<int, int>((a, b) =>
        {
            if (a > b)
            {
                var exp = new Expectation<int>(b, "b");
                try
                {
                    exp.toBeGreaterThan(a);
                    return false; // Should have thrown
                }
                catch (AssertionException)
                {
                    return true; // Expected behavior
                }
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBeLessThan_IsTransitive()
    {
        // Property: If a < b and b < c, then a < c
        Prop.ForAll<int, int, int>((a, b, c) =>
        {
            if (a < b && b < c)
            {
                var exp = new Expectation<int>(a, "a");
                exp.toBeLessThan(c);
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBeInRange_BoundaryConsistency()
    {
        // Property: value in [min, max] iff value >= min AND value <= max
        Prop.ForAll<int, int, int>((min, max, value) =>
        {
            // Ensure min <= max for valid range
            var actualMin = Math.Min(min, max);
            var actualMax = Math.Max(min, max);

            var exp = new Expectation<int>(value, "value");
            var shouldBeInRange = value >= actualMin && value <= actualMax;

            try
            {
                exp.toBeInRange(actualMin, actualMax);
                return shouldBeInRange; // Should only succeed when in range
            }
            catch (AssertionException)
            {
                return !shouldBeInRange; // Should fail when out of range
            }
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBeInRange_ContainsBoundaries()
    {
        // Property: min and max are both in the range [min, max]
        Prop.ForAll<int, int>((a, b) =>
        {
            var min = Math.Min(a, b);
            var max = Math.Max(a, b);

            var expMin = new Expectation<int>(min, "min");
            var expMax = new Expectation<int>(max, "max");

            expMin.toBeInRange(min, max);
            expMax.toBeInRange(min, max);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBeCloseTo_IsSymmetric()
    {
        // Property: If a is close to b (within tolerance), then b is close to a
        Prop.ForAll<int, int>((a, b) =>
        {
            var tolerance = Math.Abs(a - b) + 1; // Ensure tolerance includes the difference

            var expA = new Expectation<int>(a, "a");
            var expB = new Expectation<int>(b, "b");

            expA.toBeCloseTo(b, tolerance);
            expB.toBeCloseTo(a, tolerance); // Should also pass
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBeCloseTo_ZeroTolerance_RequiresExactMatch()
    {
        // Property: With tolerance 0, only identical values should pass
        Prop.ForAll<int>(value =>
        {
            var exp = new Expectation<int>(value, "value");
            exp.toBeCloseTo(value, 0);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBeAtLeast_IsReflexive()
    {
        // Property: For any value x, x >= x (x is at least x)
        Prop.ForAll<int>(value =>
        {
            var exp = new Expectation<int>(value, "value");
            exp.toBeAtLeast(value);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBeAtMost_IsReflexive()
    {
        // Property: For any value x, x <= x (x is at most x)
        Prop.ForAll<int>(value =>
        {
            var exp = new Expectation<int>(value, "value");
            exp.toBeAtMost(value);
            return true;
        }).QuickCheckThrowOnFailure();
    }
}
