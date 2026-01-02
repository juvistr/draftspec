using System.Text.RegularExpressions;

namespace DraftSpec.Cli;

/// <summary>
/// Matches strings against a pattern using regex when valid, falling back to substring match.
/// </summary>
public sealed class PatternMatcher
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

    private readonly Func<string, bool> _matchFunc;

    private PatternMatcher(Func<string, bool> matchFunc) => _matchFunc = matchFunc;

    /// <summary>
    /// Creates a PatternMatcher from a pattern string.
    /// </summary>
    public static PatternMatcher Create(string pattern, TimeSpan? timeout = null)
    {
        return Create(pattern, timeout, CreateRegex);
    }

    /// <summary>
    /// Creates a PatternMatcher with a custom regex factory (for testing).
    /// </summary>
    internal static PatternMatcher Create(
        string pattern,
        TimeSpan? timeout,
        Func<string, TimeSpan, Regex> regexFactory)
    {
        var effectiveTimeout = timeout ?? DefaultTimeout;

        try
        {
            var regex = regexFactory(pattern, effectiveTimeout);
            return new PatternMatcher(input => regex.IsMatch(input));
        }
        catch (RegexParseException)
        {
            return CreateSubstringMatcher(pattern);
        }
        catch (RegexMatchTimeoutException)
        {
            return CreateSubstringMatcher(pattern);
        }
    }

    /// <summary>
    /// Returns true if the input matches the pattern.
    /// </summary>
    public bool Matches(string input) => _matchFunc(input);

    private static PatternMatcher CreateSubstringMatcher(string pattern)
        => new(input => input.Contains(pattern, StringComparison.OrdinalIgnoreCase));

    private static Regex CreateRegex(string pattern, TimeSpan timeout)
        => new(pattern, RegexOptions.IgnoreCase, timeout);
}
