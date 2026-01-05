using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Direct unit tests for the ParsePathWithLineNumbers internal method.
/// These tests verify the edge cases in path and line number parsing that are
/// harder to test through the full Parse() method.
/// </summary>
public class ParsePathWithLineNumbersTests
{
    [Test]
    public async Task ParsePathWithLineNumbers_WindowsPathWithoutLineNumber_ReturnsFullPath()
    {
        var options = new CliOptions();
        var result = CliOptionsParser.ParsePathWithLineNumbers(@"C:\Users\test\specs\test.spec.csx", options);

        await Assert.That(result).IsEqualTo(@"C:\Users\test\specs\test.spec.csx");
        Assert.Null(options.Filter.LineFilters);
    }

    [Test]
    public async Task ParsePathWithLineNumbers_WindowsPathWithLineNumber_ExtractsBoth()
    {
        var options = new CliOptions();
        var result = CliOptionsParser.ParsePathWithLineNumbers(@"C:\Users\test\specs\test.spec.csx:42", options);

        await Assert.That(result).IsEqualTo(@"C:\Users\test\specs\test.spec.csx");
        Assert.NotNull(options.Filter.LineFilters);
        await Assert.That(options.Filter.LineFilters[0].Lines).IsEquivalentTo(new[] { 42 });
    }

    [Test]
    public async Task ParsePathWithLineNumbers_UnixPath_ExtractsCorrectly()
    {
        var options = new CliOptions();
        var result = CliOptionsParser.ParsePathWithLineNumbers("/home/user/specs/test.spec.csx:15,20", options);

        await Assert.That(result).IsEqualTo("/home/user/specs/test.spec.csx");
        await Assert.That(options.Filter.LineFilters![0].Lines).IsEquivalentTo(new[] { 15, 20 });
    }

    [Test]
    public async Task ParsePathWithLineNumbers_NoLineNumber_ReturnsPathWithNullFilters()
    {
        var options = new CliOptions();
        var result = CliOptionsParser.ParsePathWithLineNumbers("test.spec.csx", options);

        await Assert.That(result).IsEqualTo("test.spec.csx");
        Assert.Null(options.Filter.LineFilters);
    }

    [Test]
    public async Task ParsePathWithLineNumbers_InvalidLineNumber_SkipsInvalid()
    {
        var options = new CliOptions();
        // "abc" is not a digit, so the whole thing after colon should be treated as non-line-number
        var result = CliOptionsParser.ParsePathWithLineNumbers("test.spec.csx:abc", options);

        await Assert.That(result).IsEqualTo("test.spec.csx:abc");
        Assert.Null(options.Filter.LineFilters);
    }

    [Test]
    public async Task ParsePathWithLineNumbers_MultipleLineNumbers_ParsesAll()
    {
        var options = new CliOptions();
        var result = CliOptionsParser.ParsePathWithLineNumbers("test.spec.csx:10,20,30,40", options);

        await Assert.That(result).IsEqualTo("test.spec.csx");
        await Assert.That(options.Filter.LineFilters![0].Lines).IsEquivalentTo(new[] { 10, 20, 30, 40 });
    }

    [Test]
    public async Task ParsePathWithLineNumbers_ZeroLineNumber_Filtered()
    {
        var options = new CliOptions();
        var result = CliOptionsParser.ParsePathWithLineNumbers("test.spec.csx:0,15,0,20", options);

        await Assert.That(result).IsEqualTo("test.spec.csx");
        // Zero should be filtered out
        await Assert.That(options.Filter.LineFilters![0].Lines).IsEquivalentTo(new[] { 15, 20 });
    }

    [Test]
    public async Task ParsePathWithLineNumbers_EmptyAfterColon_ReturnsOriginalPath()
    {
        var options = new CliOptions();
        var result = CliOptionsParser.ParsePathWithLineNumbers("test.spec.csx:", options);

        // Empty string after colon is not digits, so return original
        await Assert.That(result).IsEqualTo("test.spec.csx:");
        Assert.Null(options.Filter.LineFilters);
    }

    [Test]
    public async Task ParsePathWithLineNumbers_MixedDigitsAndNonDigits_ReturnsOriginalPath()
    {
        var options = new CliOptions();
        var result = CliOptionsParser.ParsePathWithLineNumbers("test.spec.csx:15abc", options);

        // "15abc" contains non-digits, so not treated as line number
        await Assert.That(result).IsEqualTo("test.spec.csx:15abc");
        Assert.Null(options.Filter.LineFilters);
    }

    [Test]
    public async Task ParsePathWithLineNumbers_AllZeroLineNumbers_NoFiltersAdded()
    {
        var options = new CliOptions();
        var result = CliOptionsParser.ParsePathWithLineNumbers("test.spec.csx:0,0,0", options);

        await Assert.That(result).IsEqualTo("test.spec.csx");
        // All zeros are filtered, so no LineFilters should be added (empty array case)
        // Based on code: if (lineNumbers.Length > 0) adds filter, so no filter added
        Assert.Null(options.Filter.LineFilters);
    }

    [Test]
    public async Task ParsePathWithLineNumbers_TrailingComma_Handled()
    {
        var options = new CliOptions();
        var result = CliOptionsParser.ParsePathWithLineNumbers("test.spec.csx:15,20,", options);

        await Assert.That(result).IsEqualTo("test.spec.csx");
        await Assert.That(options.Filter.LineFilters![0].Lines).IsEquivalentTo(new[] { 15, 20 });
    }

    [Test]
    public async Task ParsePathWithLineNumbers_LeadingComma_Handled()
    {
        var options = new CliOptions();
        var result = CliOptionsParser.ParsePathWithLineNumbers("test.spec.csx:,15,20", options);

        await Assert.That(result).IsEqualTo("test.spec.csx");
        await Assert.That(options.Filter.LineFilters![0].Lines).IsEquivalentTo(new[] { 15, 20 });
    }
}
