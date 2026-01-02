using System.Text.RegularExpressions;
using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Unit tests for PatternMatcher regex-with-fallback functionality.
/// </summary>
public class PatternMatcherTests
{
    #region Valid Regex Matching

    [Test]
    public async Task Matches_ValidRegex_MatchesPattern()
    {
        var matcher = PatternMatcher.Create("^test.*$");

        await Assert.That(matcher.Matches("testing")).IsTrue();
        await Assert.That(matcher.Matches("test123")).IsTrue();
    }

    [Test]
    public async Task Matches_ValidRegex_RejectsNonMatching()
    {
        var matcher = PatternMatcher.Create("^test.*$");

        await Assert.That(matcher.Matches("other")).IsFalse();
        await Assert.That(matcher.Matches("prefix-test")).IsFalse();
    }

    [Test]
    public async Task Matches_SimplePattern_MatchesAnywhere()
    {
        var matcher = PatternMatcher.Create("calc");

        await Assert.That(matcher.Matches("Calculator")).IsTrue();
        await Assert.That(matcher.Matches("the calc is here")).IsTrue();
        await Assert.That(matcher.Matches("recalculate")).IsTrue();
    }

    #endregion

    #region Invalid Regex Fallback

    [Test]
    public async Task Matches_InvalidRegex_FallsBackToSubstring()
    {
        // Unmatched parenthesis is invalid regex
        var matcher = PatternMatcher.Create("test(");

        await Assert.That(matcher.Matches("test(value)")).IsTrue();
        await Assert.That(matcher.Matches("has test( in it")).IsTrue();
    }

    [Test]
    public async Task Matches_InvalidRegex_SubstringRejectsNonMatching()
    {
        var matcher = PatternMatcher.Create("test(");

        await Assert.That(matcher.Matches("other")).IsFalse();
        await Assert.That(matcher.Matches("testing")).IsFalse();
    }

    [Test]
    public async Task Matches_InvalidRegexBracket_FallsBackToSubstring()
    {
        // Unmatched bracket
        var matcher = PatternMatcher.Create("[incomplete");

        await Assert.That(matcher.Matches("has [incomplete bracket")).IsTrue();
        await Assert.That(matcher.Matches("other")).IsFalse();
    }

    #endregion

    #region Regex Timeout Fallback

    [Test]
    public async Task Matches_RegexTimeout_FallsBackToSubstring()
    {
        // Inject a factory that always throws timeout
        var matcher = PatternMatcher.Create(
            "pattern",
            TimeSpan.FromSeconds(1),
            (_, _) => throw new RegexMatchTimeoutException("pattern", "input", TimeSpan.Zero));

        await Assert.That(matcher.Matches("has pattern here")).IsTrue();
        await Assert.That(matcher.Matches("contains the pattern")).IsTrue();
    }

    [Test]
    public async Task Matches_RegexTimeout_SubstringRejectsNonMatching()
    {
        var matcher = PatternMatcher.Create(
            "pattern",
            TimeSpan.FromSeconds(1),
            (_, _) => throw new RegexMatchTimeoutException("pattern", "input", TimeSpan.Zero));

        await Assert.That(matcher.Matches("other")).IsFalse();
        await Assert.That(matcher.Matches("no match")).IsFalse();
    }

    #endregion

    #region Case Insensitivity

    [Test]
    public async Task Matches_CaseInsensitive_MatchesAnyCase()
    {
        var matcher = PatternMatcher.Create("TEST");

        await Assert.That(matcher.Matches("test")).IsTrue();
        await Assert.That(matcher.Matches("Test")).IsTrue();
        await Assert.That(matcher.Matches("TEST")).IsTrue();
        await Assert.That(matcher.Matches("TeSt")).IsTrue();
    }

    [Test]
    public async Task Matches_InvalidRegex_CaseInsensitive()
    {
        var matcher = PatternMatcher.Create("Test(");

        await Assert.That(matcher.Matches("TEST(")).IsTrue();
        await Assert.That(matcher.Matches("test(")).IsTrue();
    }

    [Test]
    public async Task Matches_RegexTimeout_CaseInsensitive()
    {
        var matcher = PatternMatcher.Create(
            "Pattern",
            TimeSpan.FromSeconds(1),
            (_, _) => throw new RegexMatchTimeoutException("Pattern", "input", TimeSpan.Zero));

        await Assert.That(matcher.Matches("PATTERN")).IsTrue();
        await Assert.That(matcher.Matches("pattern")).IsTrue();
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Matches_EmptyInput_ValidRegex()
    {
        var matcher = PatternMatcher.Create("^$");

        await Assert.That(matcher.Matches("")).IsTrue();
        await Assert.That(matcher.Matches("not empty")).IsFalse();
    }

    [Test]
    public async Task Matches_SpecialCharsInPattern_InvalidRegex()
    {
        // Multiple special chars that make invalid regex
        var matcher = PatternMatcher.Create("([{");

        await Assert.That(matcher.Matches("has ([{ chars")).IsTrue();
        await Assert.That(matcher.Matches("other")).IsFalse();
    }

    [Test]
    public async Task Matches_DotStarPattern_MatchesEverything()
    {
        var matcher = PatternMatcher.Create(".*");

        await Assert.That(matcher.Matches("anything")).IsTrue();
        await Assert.That(matcher.Matches("")).IsTrue();
    }

    #endregion
}
