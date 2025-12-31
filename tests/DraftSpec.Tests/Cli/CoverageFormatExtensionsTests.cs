using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Tests.Cli;

public class CoverageFormatExtensionsTests
{
    #region ParseCoverageFormat Tests

    [Test]
    [Arguments("cobertura", CoverageFormat.Cobertura)]
    [Arguments("xml", CoverageFormat.Xml)]
    [Arguments("coverage", CoverageFormat.Coverage)]
    [Arguments("COBERTURA", CoverageFormat.Cobertura)]
    [Arguments("XML", CoverageFormat.Xml)]
    [Arguments("COVERAGE", CoverageFormat.Coverage)]
    public async Task ParseCoverageFormat_ValidValues_ReturnsExpectedFormat(string input, CoverageFormat expected)
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

    #endregion

    #region TryParseCoverageFormat Tests

    [Test]
    [Arguments("cobertura", CoverageFormat.Cobertura)]
    [Arguments("xml", CoverageFormat.Xml)]
    [Arguments("coverage", CoverageFormat.Coverage)]
    public async Task TryParseCoverageFormat_ValidValues_ReturnsTrueAndParsesFormat(string input, CoverageFormat expected)
    {
        var success = input.TryParseCoverageFormat(out var result);

        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task TryParseCoverageFormat_InvalidValue_ReturnsFalse()
    {
        var success = "invalid".TryParseCoverageFormat(out var result);

        await Assert.That(success).IsFalse();
        await Assert.That(result).IsEqualTo(CoverageFormat.Cobertura); // Default
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task TryParseCoverageFormat_NullOrWhitespace_ReturnsFalse(string? input)
    {
        var success = input.TryParseCoverageFormat(out _);
        await Assert.That(success).IsFalse();
    }

    #endregion

    #region ToCliString Tests

    [Test]
    [Arguments(CoverageFormat.Cobertura, "cobertura")]
    [Arguments(CoverageFormat.Xml, "xml")]
    [Arguments(CoverageFormat.Coverage, "coverage")]
    public async Task ToCliString_AllFormats_ReturnsExpectedString(CoverageFormat format, string expected)
    {
        var result = format.ToCliString();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ToCliString_InvalidFormat_ThrowsArgumentOutOfRangeException()
    {
        var invalidFormat = (CoverageFormat)999;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        {
            invalidFormat.ToCliString();
            return Task.CompletedTask;
        });
    }

    #endregion

    #region GetFileExtension Tests

    [Test]
    [Arguments(CoverageFormat.Cobertura, "cobertura.xml")]
    [Arguments(CoverageFormat.Xml, "xml")]
    [Arguments(CoverageFormat.Coverage, "coverage")]
    public async Task GetFileExtension_AllFormats_ReturnsExpectedExtension(CoverageFormat format, string expected)
    {
        var result = format.GetFileExtension();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GetFileExtension_InvalidFormat_ThrowsArgumentOutOfRangeException()
    {
        var invalidFormat = (CoverageFormat)999;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        {
            invalidFormat.GetFileExtension();
            return Task.CompletedTask;
        });
    }

    #endregion
}
