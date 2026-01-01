using FsCheck;
using FsCheck.Fluent;

namespace DraftSpec.Tests.Properties;

/// <summary>
/// Property-based tests for StringExpectation matchers using FsCheck.
/// These tests verify string composition properties that must hold for all inputs.
/// </summary>
public class StringExpectationPropertyTests
{
    [Test]
    public void ToBe_IsReflexive()
    {
        // Property: For any non-null string, s.toBe(s) should always pass
        Prop.ForAll<NonNull<string>>(s =>
        {
            var exp = new StringExpectation(s.Get, "str");
            exp.toBe(s.Get);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToStartWith_ImpliesContains()
    {
        // Property: If a string starts with prefix, it also contains prefix
        Prop.ForAll<NonNull<string>, string>((prefix, suffix) =>
        {
            var s = prefix.Get + (suffix ?? "");
            var exp = new StringExpectation(s, "str");

            exp.toStartWith(prefix.Get);
            exp.toContain(prefix.Get); // Must also pass
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToEndWith_ImpliesContains()
    {
        // Property: If a string ends with suffix, it also contains suffix
        Prop.ForAll<string, NonNull<string>>((prefix, suffix) =>
        {
            var s = (prefix ?? "") + suffix.Get;
            var exp = new StringExpectation(s, "str");

            exp.toEndWith(suffix.Get);
            exp.toContain(suffix.Get); // Must also pass
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToContain_Composition()
    {
        // Property: Concatenation of parts contains each part
        Prop.ForAll<NonNull<string>, NonNull<string>, NonNull<string>>((a, b, c) =>
        {
            var full = a.Get + b.Get + c.Get;
            var exp = new StringExpectation(full, "str");

            exp.toContain(a.Get);
            exp.toContain(b.Get);
            exp.toContain(c.Get);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToContain_EmptyString_AlwaysPasses()
    {
        // Property: Any non-null string contains the empty string
        Prop.ForAll<NonNull<string>>(s =>
        {
            var exp = new StringExpectation(s.Get, "str");
            exp.toContain(""); // Should always pass
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToStartWith_EmptyString_AlwaysPasses()
    {
        // Property: Any non-null string starts with the empty string
        Prop.ForAll<NonNull<string>>(s =>
        {
            var exp = new StringExpectation(s.Get, "str");
            exp.toStartWith(""); // Should always pass
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToEndWith_EmptyString_AlwaysPasses()
    {
        // Property: Any non-null string ends with the empty string
        Prop.ForAll<NonNull<string>>(s =>
        {
            var exp = new StringExpectation(s.Get, "str");
            exp.toEndWith(""); // Should always pass
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToHaveLength_MatchesActualLength()
    {
        // Property: toHaveLength passes when given actual length
        Prop.ForAll<NonNull<string>>(s =>
        {
            var exp = new StringExpectation(s.Get, "str");
            exp.toHaveLength(s.Get.Length);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToStartWith_Self_AlwaysPasses()
    {
        // Property: Any non-null string starts with itself
        Prop.ForAll<NonNull<string>>(s =>
        {
            var exp = new StringExpectation(s.Get, "str");
            exp.toStartWith(s.Get);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToEndWith_Self_AlwaysPasses()
    {
        // Property: Any non-null string ends with itself
        Prop.ForAll<NonNull<string>>(s =>
        {
            var exp = new StringExpectation(s.Get, "str");
            exp.toEndWith(s.Get);
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ToContain_Self_AlwaysPasses()
    {
        // Property: Any non-null string contains itself
        Prop.ForAll<NonNull<string>>(s =>
        {
            var exp = new StringExpectation(s.Get, "str");
            exp.toContain(s.Get);
            return true;
        }).QuickCheckThrowOnFailure();
    }
}
