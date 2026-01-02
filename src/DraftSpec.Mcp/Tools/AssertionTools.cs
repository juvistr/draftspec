using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using DraftSpec.Formatters;
using ModelContextProtocol.Server;

namespace DraftSpec.Mcp.Tools;

/// <summary>
/// MCP tools for parsing natural language assertions.
/// </summary>
[McpServerToolType]
public static partial class AssertionTools
{
    /// <summary>
    /// Parse a natural language assertion into DraftSpec expect() syntax.
    /// </summary>
    [McpServerTool(Name = "parse_assertion")]
    [Description("Convert a natural language assertion to DraftSpec expect() syntax. " +
                 "Parses common assertion patterns and returns the equivalent code.")]
    public static string ParseAssertion(
        [Description("Natural language assertion (e.g., 'should be greater than 5', 'should contain hello')")]
        string naturalLanguage,
        [Description("Variable or expression name to assert on (e.g., 'result', 'user.Name', 'items.Count')")]
        string variableName)
    {
        var result = Parse(naturalLanguage, variableName);
        return JsonSerializer.Serialize(result, JsonOptionsProvider.Default);
    }

    /// <summary>
    /// Parse natural language assertion to structured result.
    /// </summary>
    internal static AssertionParseResult Parse(string naturalLanguage, string variableName)
    {
        var input = naturalLanguage.Trim();

        // Try each pattern in order
        foreach (var pattern in Patterns)
        {
            var match = pattern.Regex.Match(input);
            if (match.Success)
            {
                var code = pattern.Generator(variableName, match);
                return new AssertionParseResult
                {
                    Success = true,
                    Code = code,
                    Confidence = pattern.Confidence,
                    Pattern = pattern.Description
                };
            }
        }

        // No pattern matched
        return new AssertionParseResult
        {
            Success = false,
            Code = $"expect({variableName}).toBe(/* TODO: specify expected value */)",
            Confidence = 0.0,
            Pattern = null,
            Error = $"Could not parse assertion: '{naturalLanguage}'. Using fallback template."
        };
    }

    private static readonly AssertionPattern[] Patterns =
    [
        // Null checks
        new(
            NotBeNullRegex(),
            "not be null",
            1.0,
            (v, _) => $"expect({v}).toNotBeNull()"),
        new(
            BeNullRegex(),
            "be null",
            1.0,
            (v, _) => $"expect({v}).toBeNull()"),

        // Boolean checks
        new(
            BeTrueRegex(),
            "be true",
            1.0,
            (v, _) => $"expect({v}).toBeTrue()"),
        new(
            BeFalseRegex(),
            "be false",
            1.0,
            (v, _) => $"expect({v}).toBeFalse()"),

        // Numeric comparisons
        new(
            BeGreaterThanRegex(),
            "be greater than",
            0.95,
            (v, m) => $"expect({v}).toBeGreaterThan({m.Groups["value"].Value})"),
        new(
            BeLessThanRegex(),
            "be less than",
            0.95,
            (v, m) => $"expect({v}).toBeLessThan({m.Groups["value"].Value})"),
        new(
            BeAtLeastRegex(),
            "be at least / be >=",
            0.95,
            (v, m) => $"expect({v}).toBeAtLeast({m.Groups["value"].Value})"),
        new(
            BeAtMostRegex(),
            "be at most / be <=",
            0.95,
            (v, m) => $"expect({v}).toBeAtMost({m.Groups["value"].Value})"),

        // Collection count
        new(
            HaveCountRegex(),
            "have count",
            0.9,
            (v, m) => $"expect({v}).toHaveCount({m.Groups["count"].Value})"),
        new(
            HaveItemsRegex(),
            "have N items",
            0.9,
            (v, m) => $"expect({v}).toHaveCount({m.Groups["count"].Value})"),

        // Empty checks
        new(
            NotBeEmptyRegex(),
            "not be empty",
            1.0,
            (v, _) => $"expect({v}).toNotBeEmpty()"),
        new(
            BeEmptyRegex(),
            "be empty",
            1.0,
            (v, _) => $"expect({v}).toBeEmpty()"),

        // String operations
        new(
            ContainStringRegex(),
            "contain string",
            0.9,
            (v, m) => $"expect({v}).toContain(\"{EscapeString(m.Groups["text"].Value)}\")"),
        new(
            StartWithRegex(),
            "start with",
            0.9,
            (v, m) => $"expect({v}).toStartWith(\"{EscapeString(m.Groups["text"].Value)}\")"),
        new(
            EndWithRegex(),
            "end with",
            0.9,
            (v, m) => $"expect({v}).toEndWith(\"{EscapeString(m.Groups["text"].Value)}\")"),
        new(
            MatchPatternRegex(),
            "match pattern",
            0.85,
            (v, m) => $"expect({v}).toMatch(\"{EscapeString(m.Groups["pattern"].Value)}\")"),

        // Exception handling - order matters!
        new(
            NotThrowRegex(),
            "not throw",
            1.0,
            (v, _) => $"expect(() => {v}).toNotThrow()"),
        new(
            ThrowGenericRegex(),
            "throw (generic)",
            0.95,
            (v, _) => $"expect(() => {v}).toThrow()"),
        new(
            ThrowExceptionRegex(),
            "throw specific exception",
            0.9,
            (v, m) => $"expect(() => {v}).toThrow<{FormatExceptionType(m.Groups["exception"].Value)}>()"),

        // Equality (should be last as it's most general)
        new(
            NotEqualRegex(),
            "not equal",
            0.85,
            (v, m) => $"expect({v}).not.toBe({FormatValue(m.Groups["value"].Value)})"),
        new(
            EqualRegex(),
            "equal / be",
            0.8,
            (v, m) => $"expect({v}).toBe({FormatValue(m.Groups["value"].Value)})"),
    ];

    private static string EscapeString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string FormatValue(string value)
    {
        // Check if it's a number
        if (double.TryParse(value, out _))
            return value;

        // Check if it's a boolean
        if (value is "true" or "false")
            return value;

        // Check if it's already quoted
        if ((value.StartsWith('"') && value.EndsWith('"')) ||
            (value.StartsWith('\'') && value.EndsWith('\'')))
            return $"\"{EscapeString(value[1..^1])}\"";

        // Treat as string
        return $"\"{EscapeString(value)}\"";
    }

    private static string FormatExceptionType(string exceptionName)
    {
        var name = exceptionName.Trim();

        // Remove common prefixes
        if (name.StartsWith("an ", StringComparison.OrdinalIgnoreCase))
            name = name[3..];
        else if (name.StartsWith("a ", StringComparison.OrdinalIgnoreCase))
            name = name[2..];

        // Normalize to PascalCase and add Exception suffix if needed
        name = string.Join("", name.Split(' ')
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));

        if (!name.EndsWith("Exception", StringComparison.OrdinalIgnoreCase))
            name += "Exception";

        return name;
    }

    // Regex patterns using source generators with case-insensitive matching and NonBacktracking for ReDoS protection
    [GeneratedRegex(@"^(?:should\s+)?not\s+be\s+null$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex NotBeNullRegex();

    [GeneratedRegex(@"^(?:should\s+)?be\s+null$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex BeNullRegex();

    [GeneratedRegex(@"^(?:should\s+)?be\s+true$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex BeTrueRegex();

    [GeneratedRegex(@"^(?:should\s+)?be\s+false$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex BeFalseRegex();

    [GeneratedRegex(@"^(?:should\s+)?be\s+greater\s+than\s+(?<value>-?\d+(?:\.\d+)?)$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex BeGreaterThanRegex();

    [GeneratedRegex(@"^(?:should\s+)?be\s+less\s+than\s+(?<value>-?\d+(?:\.\d+)?)$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex BeLessThanRegex();

    [GeneratedRegex(@"^(?:should\s+)?(?:be\s+(?:at\s+least|>=)|>=)\s*(?<value>-?\d+(?:\.\d+)?)$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex BeAtLeastRegex();

    [GeneratedRegex(@"^(?:should\s+)?(?:be\s+(?:at\s+most|<=)|<=)\s*(?<value>-?\d+(?:\.\d+)?)$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex BeAtMostRegex();

    [GeneratedRegex(@"^(?:should\s+)?have\s+(?:a\s+)?count\s+(?:of\s+)?(?<count>\d+)$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex HaveCountRegex();

    [GeneratedRegex(@"^(?:should\s+)?have\s+(?<count>\d+)\s+items?$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex HaveItemsRegex();

    [GeneratedRegex(@"^(?:should\s+)?not\s+be\s+empty$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex NotBeEmptyRegex();

    [GeneratedRegex(@"^(?:should\s+)?be\s+empty$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex BeEmptyRegex();

    [GeneratedRegex(@"^(?:should\s+)?contain\s+['""]?(?<text>[^'""]+)['""]?$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex ContainStringRegex();

    [GeneratedRegex(@"^(?:should\s+)?start\s+with\s+['""]?(?<text>[^'""]+)['""]?$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex StartWithRegex();

    [GeneratedRegex(@"^(?:should\s+)?end\s+with\s+['""]?(?<text>[^'""]+)['""]?$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex EndWithRegex();

    [GeneratedRegex(@"^(?:should\s+)?match\s+(?:pattern\s+)?['""]?(?<pattern>[^'""]+)['""]?$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex MatchPatternRegex();

    [GeneratedRegex(@"^(?:should\s+)?not\s+throw(?:\s+(?:any\s+)?(?:exception|error))?$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex NotThrowRegex();

    // Matches "throw", "throw exception", "throw an exception", "throw a exception"
    [GeneratedRegex(@"^(?:should\s+)?throw(?:\s+(?:an?\s+)?exception)?$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex ThrowGenericRegex();

    // Matches "throw <ExceptionType>" where type is NOT just "exception"
    // Excludes: "an exception", "a exception", just "exception"
    // Uses negative lookahead which is not compatible with NonBacktracking but safe (no nested quantifiers)
#pragma warning disable MA0009 // Pattern uses lookahead which is not compatible with NonBacktracking
    [GeneratedRegex(@"^(?:should\s+)?throw\s+(?<exception>(?!an?\s+exception$|exception$)[A-Za-z]+(?:\s+[A-Za-z]+)*(?:Exception)?)$", RegexOptions.IgnoreCase)]
    private static partial Regex ThrowExceptionRegex();
#pragma warning restore MA0009

    [GeneratedRegex(@"^(?:should\s+)?(?:not\s+(?:be\s+)?equal(?:\s+to)?|not\s+equal)\s+(?<value>.+)$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex NotEqualRegex();

    [GeneratedRegex(@"^(?:should\s+)?(?:(?:be\s+)?equal(?:\s+to)?|be)\s+(?<value>.+)$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex EqualRegex();

    private record AssertionPattern(
        Regex Regex,
        string Description,
        double Confidence,
        Func<string, Match, string> Generator);
}
