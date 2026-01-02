using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Tests.Cli;

public class CoverageMapFormatExtensionsTests
{
    #region ParseCoverageMapFormat Tests

    [Test]
    [Arguments("console", CoverageMapFormat.Console)]
    [Arguments("json", CoverageMapFormat.Json)]
    [Arguments("CONSOLE", CoverageMapFormat.Console)]
    [Arguments("JSON", CoverageMapFormat.Json)]
    [Arguments("Console", CoverageMapFormat.Console)]
    [Arguments("Json", CoverageMapFormat.Json)]
    public async Task ParseCoverageMapFormat_ValidValues_ReturnsExpectedFormat(string input, CoverageMapFormat expected)
    {
        var result = input.ParseCoverageMapFormat();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ParseCoverageMapFormat_InvalidValue_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            "invalid".ParseCoverageMapFormat();
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task ParseCoverageMapFormat_InvalidValue_ExceptionContainsValidOptions()
    {
        try
        {
            "xml".ParseCoverageMapFormat();
            Assert.Fail("Expected ArgumentException");
        }
        catch (ArgumentException ex)
        {
            await Assert.That(ex.Message).Contains("console");
            await Assert.That(ex.Message).Contains("json");
        }
    }

    #endregion

    #region TryParseCoverageMapFormat Tests

    [Test]
    [Arguments("console", CoverageMapFormat.Console)]
    [Arguments("json", CoverageMapFormat.Json)]
    public async Task TryParseCoverageMapFormat_ValidValues_ReturnsTrueAndParsesFormat(string input, CoverageMapFormat expected)
    {
        var success = input.TryParseCoverageMapFormat(out var result);

        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task TryParseCoverageMapFormat_InvalidValue_ReturnsFalse()
    {
        var success = "invalid".TryParseCoverageMapFormat(out var result);

        await Assert.That(success).IsFalse();
        await Assert.That(result).IsEqualTo(CoverageMapFormat.Console); // Default
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task TryParseCoverageMapFormat_NullOrWhitespace_ReturnsFalse(string? input)
    {
        var success = input.TryParseCoverageMapFormat(out _);
        await Assert.That(success).IsFalse();
    }

    #endregion

    #region ToCliString Tests

    [Test]
    [Arguments(CoverageMapFormat.Console, "console")]
    [Arguments(CoverageMapFormat.Json, "json")]
    public async Task ToCliString_AllFormats_ReturnsExpectedString(CoverageMapFormat format, string expected)
    {
        var result = format.ToCliString();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ToCliString_InvalidFormat_ThrowsArgumentOutOfRangeException()
    {
        var invalidFormat = (CoverageMapFormat)999;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        {
            invalidFormat.ToCliString();
            return Task.CompletedTask;
        });
    }

    #endregion
}
