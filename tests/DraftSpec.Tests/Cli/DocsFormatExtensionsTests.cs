using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Tests.Cli;

public class DocsFormatExtensionsTests
{
    #region ParseDocsFormat Tests

    [Test]
    [Arguments("markdown", DocsFormat.Markdown)]
    [Arguments("md", DocsFormat.Markdown)]
    [Arguments("html", DocsFormat.Html)]
    [Arguments("MARKDOWN", DocsFormat.Markdown)]
    [Arguments("MD", DocsFormat.Markdown)]
    [Arguments("HTML", DocsFormat.Html)]
    public async Task ParseDocsFormat_ValidValues_ReturnsExpectedFormat(string input, DocsFormat expected)
    {
        var result = input.ParseDocsFormat();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ParseDocsFormat_InvalidValue_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            "invalid".ParseDocsFormat();
            return Task.CompletedTask;
        });
    }

    #endregion

    #region TryParseDocsFormat Tests

    [Test]
    [Arguments("markdown", DocsFormat.Markdown)]
    [Arguments("md", DocsFormat.Markdown)]
    [Arguments("html", DocsFormat.Html)]
    public async Task TryParseDocsFormat_ValidValues_ReturnsTrueAndParsesFormat(string input, DocsFormat expected)
    {
        var success = input.TryParseDocsFormat(out var result);

        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task TryParseDocsFormat_InvalidValue_ReturnsFalse()
    {
        var success = "invalid".TryParseDocsFormat(out var result);

        await Assert.That(success).IsFalse();
        await Assert.That(result).IsEqualTo(DocsFormat.Markdown); // Default
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task TryParseDocsFormat_NullOrWhitespace_ReturnsFalse(string? input)
    {
        var success = input.TryParseDocsFormat(out _);
        await Assert.That(success).IsFalse();
    }

    #endregion

    #region ToCliString Tests

    [Test]
    [Arguments(DocsFormat.Markdown, "markdown")]
    [Arguments(DocsFormat.Html, "html")]
    public async Task ToCliString_AllFormats_ReturnsExpectedString(DocsFormat format, string expected)
    {
        var result = format.ToCliString();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ToCliString_InvalidFormat_ThrowsArgumentOutOfRangeException()
    {
        var invalidFormat = (DocsFormat)999;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        {
            invalidFormat.ToCliString();
            return Task.CompletedTask;
        });
    }

    #endregion
}
