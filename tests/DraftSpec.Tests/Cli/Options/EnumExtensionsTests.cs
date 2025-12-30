using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Tests.Cli.Options;

/// <summary>
/// Tests for enum extension methods (Parse, TryParse, ToCliString).
/// </summary>
public class EnumExtensionsTests
{
    #region OutputFormat

    [Test]
    [Arguments("console", OutputFormat.Console)]
    [Arguments("json", OutputFormat.Json)]
    [Arguments("markdown", OutputFormat.Markdown)]
    [Arguments("html", OutputFormat.Html)]
    [Arguments("junit", OutputFormat.JUnit)]
    [Arguments("CONSOLE", OutputFormat.Console)]
    [Arguments("JSON", OutputFormat.Json)]
    [Arguments("Html", OutputFormat.Html)]
    public async Task ParseOutputFormat_ValidValues_ReturnsCorrectEnum(string input, OutputFormat expected)
    {
        var result = input.ParseOutputFormat();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ParseOutputFormat_InvalidValue_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            "invalid".ParseOutputFormat();
            return Task.CompletedTask;
        });
    }

    [Test]
    [Arguments("console", true, OutputFormat.Console)]
    [Arguments("json", true, OutputFormat.Json)]
    [Arguments("invalid", false, OutputFormat.Console)]
    [Arguments("", false, OutputFormat.Console)]
    [Arguments(null, false, OutputFormat.Console)]
    public async Task TryParseOutputFormat_ReturnsExpectedResult(string? input, bool expectedSuccess, OutputFormat expectedFormat)
    {
        var success = input.TryParseOutputFormat(out var format);
        await Assert.That(success).IsEqualTo(expectedSuccess);
        await Assert.That(format).IsEqualTo(expectedFormat);
    }

    [Test]
    [Arguments(OutputFormat.Console, "console")]
    [Arguments(OutputFormat.Json, "json")]
    [Arguments(OutputFormat.Markdown, "markdown")]
    [Arguments(OutputFormat.Html, "html")]
    [Arguments(OutputFormat.JUnit, "junit")]
    public async Task OutputFormat_ToCliString_ReturnsCorrectString(OutputFormat format, string expected)
    {
        var result = format.ToCliString();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task OutputFormat_ToCliString_InvalidValue_ThrowsArgumentOutOfRangeException()
    {
        var invalidFormat = (OutputFormat)999;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        {
            invalidFormat.ToCliString();
            return Task.CompletedTask;
        });
    }

    #endregion

    #region ListFormat

    [Test]
    [Arguments("tree", ListFormat.Tree)]
    [Arguments("flat", ListFormat.Flat)]
    [Arguments("json", ListFormat.Json)]
    [Arguments("TREE", ListFormat.Tree)]
    [Arguments("Flat", ListFormat.Flat)]
    public async Task ParseListFormat_ValidValues_ReturnsCorrectEnum(string input, ListFormat expected)
    {
        var result = input.ParseListFormat();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ParseListFormat_InvalidValue_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            "invalid".ParseListFormat();
            return Task.CompletedTask;
        });
    }

    [Test]
    [Arguments("tree", true, ListFormat.Tree)]
    [Arguments("flat", true, ListFormat.Flat)]
    [Arguments("json", true, ListFormat.Json)]
    [Arguments("invalid", false, ListFormat.Tree)]
    [Arguments("", false, ListFormat.Tree)]
    [Arguments(null, false, ListFormat.Tree)]
    public async Task TryParseListFormat_ReturnsExpectedResult(string? input, bool expectedSuccess, ListFormat expectedFormat)
    {
        var success = input.TryParseListFormat(out var format);
        await Assert.That(success).IsEqualTo(expectedSuccess);
        await Assert.That(format).IsEqualTo(expectedFormat);
    }

    [Test]
    [Arguments(ListFormat.Tree, "tree")]
    [Arguments(ListFormat.Flat, "flat")]
    [Arguments(ListFormat.Json, "json")]
    public async Task ListFormat_ToCliString_ReturnsCorrectString(ListFormat format, string expected)
    {
        var result = format.ToCliString();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ListFormat_ToCliString_InvalidValue_ThrowsArgumentOutOfRangeException()
    {
        var invalidFormat = (ListFormat)999;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        {
            invalidFormat.ToCliString();
            return Task.CompletedTask;
        });
    }

    #endregion

    #region CoverageFormat

    [Test]
    [Arguments("cobertura", CoverageFormat.Cobertura)]
    [Arguments("xml", CoverageFormat.Xml)]
    [Arguments("coverage", CoverageFormat.Coverage)]
    [Arguments("COBERTURA", CoverageFormat.Cobertura)]
    [Arguments("XML", CoverageFormat.Xml)]
    public async Task ParseCoverageFormat_ValidValues_ReturnsCorrectEnum(string input, CoverageFormat expected)
    {
        var result = input.ParseCoverageFormat();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ParseCoverageFormat_InvalidValue_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            "invalid".ParseCoverageFormat();
            return Task.CompletedTask;
        });
    }

    [Test]
    [Arguments("cobertura", true, CoverageFormat.Cobertura)]
    [Arguments("xml", true, CoverageFormat.Xml)]
    [Arguments("coverage", true, CoverageFormat.Coverage)]
    [Arguments("invalid", false, CoverageFormat.Cobertura)]
    [Arguments("", false, CoverageFormat.Cobertura)]
    [Arguments(null, false, CoverageFormat.Cobertura)]
    public async Task TryParseCoverageFormat_ReturnsExpectedResult(string? input, bool expectedSuccess, CoverageFormat expectedFormat)
    {
        var success = input.TryParseCoverageFormat(out var format);
        await Assert.That(success).IsEqualTo(expectedSuccess);
        await Assert.That(format).IsEqualTo(expectedFormat);
    }

    [Test]
    [Arguments(CoverageFormat.Cobertura, "cobertura")]
    [Arguments(CoverageFormat.Xml, "xml")]
    [Arguments(CoverageFormat.Coverage, "coverage")]
    public async Task CoverageFormat_ToCliString_ReturnsCorrectString(CoverageFormat format, string expected)
    {
        var result = format.ToCliString();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task CoverageFormat_ToCliString_InvalidValue_ThrowsArgumentOutOfRangeException()
    {
        var invalidFormat = (CoverageFormat)999;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        {
            invalidFormat.ToCliString();
            return Task.CompletedTask;
        });
    }

    #endregion

    #region PartitionStrategy

    [Test]
    [Arguments("file", PartitionStrategy.File)]
    [Arguments("spec-count", PartitionStrategy.SpecCount)]
    [Arguments("FILE", PartitionStrategy.File)]
    [Arguments("SPEC-COUNT", PartitionStrategy.SpecCount)]
    public async Task ParsePartitionStrategy_ValidValues_ReturnsCorrectEnum(string input, PartitionStrategy expected)
    {
        var result = input.ParsePartitionStrategy();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ParsePartitionStrategy_InvalidValue_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            "invalid".ParsePartitionStrategy();
            return Task.CompletedTask;
        });
    }

    [Test]
    [Arguments("file", true, PartitionStrategy.File)]
    [Arguments("spec-count", true, PartitionStrategy.SpecCount)]
    [Arguments("invalid", false, PartitionStrategy.File)]
    [Arguments("", false, PartitionStrategy.File)]
    [Arguments(null, false, PartitionStrategy.File)]
    public async Task TryParsePartitionStrategy_ReturnsExpectedResult(string? input, bool expectedSuccess, PartitionStrategy expectedFormat)
    {
        var success = input.TryParsePartitionStrategy(out var format);
        await Assert.That(success).IsEqualTo(expectedSuccess);
        await Assert.That(format).IsEqualTo(expectedFormat);
    }

    [Test]
    [Arguments(PartitionStrategy.File, "file")]
    [Arguments(PartitionStrategy.SpecCount, "spec-count")]
    public async Task PartitionStrategy_ToCliString_ReturnsCorrectString(PartitionStrategy format, string expected)
    {
        var result = format.ToCliString();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task PartitionStrategy_ToCliString_InvalidValue_ThrowsArgumentOutOfRangeException()
    {
        var invalidFormat = (PartitionStrategy)999;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        {
            invalidFormat.ToCliString();
            return Task.CompletedTask;
        });
    }

    #endregion
}
