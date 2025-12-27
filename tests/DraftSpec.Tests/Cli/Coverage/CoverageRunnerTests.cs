using DraftSpec.Cli.Coverage;

namespace DraftSpec.Tests.Cli.Coverage;

/// <summary>
/// Tests for CoverageRunner pure helper methods.
/// </summary>
public class CoverageRunnerTests
{
    #region GetFileExtension

    [Test]
    public async Task GetFileExtension_Cobertura_ReturnsXmlExtension()
    {
        var runner = new CoverageRunner("/tmp", "cobertura");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("cobertura.xml");
    }

    [Test]
    public async Task GetFileExtension_Xml_ReturnsXml()
    {
        var runner = new CoverageRunner("/tmp", "xml");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("xml");
    }

    [Test]
    public async Task GetFileExtension_Coverage_ReturnsCoverage()
    {
        var runner = new CoverageRunner("/tmp", "coverage");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("coverage");
    }

    [Test]
    public async Task GetFileExtension_Unknown_DefaultsToCobertura()
    {
        var runner = new CoverageRunner("/tmp", "unknown");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("cobertura.xml");
    }

    [Test]
    public async Task GetFileExtension_IsCaseInsensitive()
    {
        var runner = new CoverageRunner("/tmp", "COBERTURA");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("cobertura.xml");
    }

    #endregion

    #region BuildDotnetCommand

    [Test]
    public async Task BuildDotnetCommand_SimpleArgs_JoinsWithSpaces()
    {
        var args = new[] { "test", "MyProject.csproj" };

        var command = CoverageRunner.BuildDotnetCommand(args);

        await Assert.That(command).IsEqualTo("dotnet test MyProject.csproj");
    }

    [Test]
    public async Task BuildDotnetCommand_EmptyArgs_ReturnsJustDotnet()
    {
        var args = Array.Empty<string>();

        var command = CoverageRunner.BuildDotnetCommand(args);

        await Assert.That(command).IsEqualTo("dotnet ");
    }

    [Test]
    public async Task BuildDotnetCommand_WithSpaces_QuotesArgs()
    {
        var args = new[] { "test", "My Project.csproj" };

        var command = CoverageRunner.BuildDotnetCommand(args);

        await Assert.That(command).IsEqualTo("dotnet test \"My Project.csproj\"");
    }

    [Test]
    public async Task BuildDotnetCommand_MixedArgs_QuotesOnlyNeeded()
    {
        var args = new[] { "test", "--filter", "Category=Integration Tests" };

        var command = CoverageRunner.BuildDotnetCommand(args);

        await Assert.That(command).IsEqualTo("dotnet test --filter \"Category=Integration Tests\"");
    }

    #endregion

    #region QuoteIfNeeded

    [Test]
    public async Task QuoteIfNeeded_SimpleString_NoQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("simple");

        await Assert.That(result).IsEqualTo("simple");
    }

    [Test]
    public async Task QuoteIfNeeded_EmptyString_ReturnsEmptyQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("");

        await Assert.That(result).IsEqualTo("\"\"");
    }

    [Test]
    public async Task QuoteIfNeeded_NullString_ReturnsEmptyQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded(null!);

        await Assert.That(result).IsEqualTo("\"\"");
    }

    [Test]
    public async Task QuoteIfNeeded_WithSpaces_AddsQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("has spaces");

        await Assert.That(result).IsEqualTo("\"has spaces\"");
    }

    [Test]
    public async Task QuoteIfNeeded_WithQuotes_EscapesAndQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("say \"hello\"");

        await Assert.That(result).IsEqualTo("\"say \\\"hello\\\"\"");
    }

    [Test]
    public async Task QuoteIfNeeded_WithBackslash_EscapesAndQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("path\\to\\file");

        await Assert.That(result).IsEqualTo("\"path\\\\to\\\\file\"");
    }

    [Test]
    public async Task QuoteIfNeeded_WithMultipleSpecialChars_EscapesAll()
    {
        var result = CoverageRunner.QuoteIfNeeded("path with \"quotes\" and \\slashes");

        await Assert.That(result).IsEqualTo("\"path with \\\"quotes\\\" and \\\\slashes\"");
    }

    [Test]
    public async Task QuoteIfNeeded_PathLikeString_NoQuotesNeeded()
    {
        var result = CoverageRunner.QuoteIfNeeded("/usr/local/bin");

        await Assert.That(result).IsEqualTo("/usr/local/bin");
    }

    [Test]
    public async Task QuoteIfNeeded_DotnetArgs_NoQuotesNeeded()
    {
        var result = CoverageRunner.QuoteIfNeeded("--configuration=Release");

        await Assert.That(result).IsEqualTo("--configuration=Release");
    }

    #endregion

    #region Constructor and Properties

    [Test]
    public async Task Constructor_SetsOutputDirectory()
    {
        using var runner = new CoverageRunner("/custom/path", "cobertura");

        await Assert.That(runner.OutputDirectory).IsEqualTo("/custom/path");
    }

    [Test]
    public async Task Constructor_SetsFormat()
    {
        using var runner = new CoverageRunner("/tmp", "xml");

        await Assert.That(runner.Format).IsEqualTo("xml");
    }

    [Test]
    public async Task Constructor_DefaultsToCobertura()
    {
        using var runner = new CoverageRunner("/tmp");

        await Assert.That(runner.Format).IsEqualTo("cobertura");
    }

    [Test]
    public async Task Constructor_NormalizesFormatToLowercase()
    {
        using var runner = new CoverageRunner("/tmp", "COBERTURA");

        await Assert.That(runner.Format).IsEqualTo("cobertura");
    }

    [Test]
    public async Task Constructor_GeneratesSessionId()
    {
        using var runner = new CoverageRunner("/tmp");

        await Assert.That(runner.SessionId).StartsWith("draftspec-");
        await Assert.That(runner.SessionId.Length).IsGreaterThan(20);
    }

    [Test]
    public async Task Constructor_SetsUniqueSessions()
    {
        using var runner1 = new CoverageRunner("/tmp");
        using var runner2 = new CoverageRunner("/tmp");

        await Assert.That(runner1.SessionId).IsNotEqualTo(runner2.SessionId);
    }

    [Test]
    public async Task Constructor_SetsCoverageFilePath()
    {
        using var runner = new CoverageRunner("/tmp/coverage", "cobertura");

        await Assert.That(runner.CoverageFile).IsEqualTo("/tmp/coverage/coverage.cobertura.xml");
    }

    [Test]
    public async Task IsServerRunning_InitiallyFalse()
    {
        using var runner = new CoverageRunner("/tmp");

        await Assert.That(runner.IsServerRunning).IsFalse();
    }

    [Test]
    public async Task GetCoverageFile_ReturnsNullWhenFileDoesNotExist()
    {
        using var runner = new CoverageRunner("/tmp/nonexistent");

        await Assert.That(runner.GetCoverageFile()).IsNull();
    }

    [Test]
    public async Task Shutdown_ReturnsFalseWhenServerNotStarted()
    {
        using var runner = new CoverageRunner("/tmp");

        var result = runner.Shutdown();

        await Assert.That(result).IsFalse();
    }

    #endregion
}
