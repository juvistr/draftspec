using FsCheck;
using FsCheck.Fluent;

namespace DraftSpec.Tests.Properties;

/// <summary>
/// Property-based tests for CollectionExpectation<T> matchers using FsCheck.
/// These tests verify collection properties that must hold for all inputs.
/// </summary>
public class CollectionExpectationPropertyTests
{
    [Test]
    public void ToHaveCount_MatchesActualLength()
    {
        // Property: toHaveCount passes when given the actual length
        Prop.ForAll<int[]>(arr =>
        {
            var exp = new CollectionExpectation<int>(arr, "arr");
            exp.toHaveCount(arr.Length);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToContain_WhenElementExists()
    {
        // Property: If array contains element, toContain passes
        Prop.ForAll<int[], int>((arr, element) =>
        {
            if (arr.Contains(element))
            {
                var exp = new CollectionExpectation<int>(arr, "arr");
                exp.toContain(element);
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToContain_Monotonic()
    {
        // Property: If A contains X, then A.Concat(B) also contains X
        Prop.ForAll<int[], int[], int>((a, b, x) =>
        {
            if (a.Contains(x))
            {
                var combined = a.Concat(b).ToArray();
                var exp = new CollectionExpectation<int>(combined, "combined");
                exp.toContain(x);
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToContainExactly_IsReflexive()
    {
        // Property: Any array contains exactly itself
        Prop.ForAll<int[]>(arr =>
        {
            var exp = new CollectionExpectation<int>(arr, "arr");
            exp.toContainExactly(arr);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBe_IsReflexive()
    {
        // Property: Any array equals itself (sequence equality)
        Prop.ForAll<int[]>(arr =>
        {
            var exp = new CollectionExpectation<int>(arr, "arr");
            exp.toBe(arr);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBeEmpty_ForEmptyCollection()
    {
        // Property: Empty array is always empty
        var exp = new CollectionExpectation<int>(Array.Empty<int>(), "arr");
        exp.toBeEmpty();
    }

    [Test]
    public void ToBeEmpty_FailsForNonEmpty()
    {
        // Property: Non-empty arrays fail toBeEmpty
        Prop.ForAll<NonEmptyArray<int>>(arr =>
        {
            var exp = new CollectionExpectation<int>(arr.Get, "arr");
            try
            {
                exp.toBeEmpty();
                return false; // Should have thrown
            }
            catch (AssertionException)
            {
                return true; // Expected
            }
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToContainAll_SubsetProperty()
    {
        // Property: Any array contains all of its elements
        Prop.ForAll<int[]>(arr =>
        {
            if (arr.Length > 0)
            {
                var exp = new CollectionExpectation<int>(arr, "arr");
                exp.toContainAll(arr);
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToHaveCount_Zero_ForEmpty()
    {
        // Property: Empty collections have count 0
        var exp = new CollectionExpectation<int>(Array.Empty<int>(), "arr");
        exp.toHaveCount(0);
    }

    [Test]
    public void ToContainExactly_OrderIndependent()
    {
        // Property: toContainExactly is order-independent
        Prop.ForAll<int[]>(arr =>
        {
            if (arr.Length > 1)
            {
                var reversed = arr.Reverse().ToArray();
                var exp = new CollectionExpectation<int>(arr, "arr");
                exp.toContainExactly(reversed); // Should pass regardless of order
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToContainExactly_DuplicateSensitive()
    {
        // Property: Duplicates matter - [1,1,2] does NOT contain exactly [1,2]
        Prop.ForAll<int>(x =>
        {
            var arrWithDup = new[] { x, x };
            var arrWithoutDup = new[] { x };

            var exp = new CollectionExpectation<int>(arrWithDup, "arr");
            try
            {
                exp.toContainExactly(arrWithoutDup);
                return false; // Should have thrown due to count mismatch
            }
            catch (AssertionException)
            {
                return true; // Expected
            }
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToBe_SequenceMatters()
    {
        // Property: toBe is order-dependent - [1,2] != [2,1]
        Prop.ForAll<int, int>((a, b) =>
        {
            if (a != b) // Need distinct elements
            {
                var arr1 = new[] { a, b };
                var arr2 = new[] { b, a };

                var exp = new CollectionExpectation<int>(arr1, "arr");
                try
                {
                    exp.toBe(arr2);
                    return false; // Should have thrown
                }
                catch (AssertionException)
                {
                    return true; // Expected - sequences are different
                }
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }
}
