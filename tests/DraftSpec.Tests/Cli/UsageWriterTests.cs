using DraftSpec.Cli;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for UsageWriter.
/// </summary>
public class UsageWriterTests
{
    private MockConsole _console = null!;
    private UsageWriter _writer = null!;

    [Before(Test)]
    public void Setup()
    {
        _console = new MockConsole();
        _writer = new UsageWriter(_console);
    }

    #region Constructor

    [Test]
    public void Constructor_NullConsole_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new UsageWriter(null!));
    }

    #endregion

    #region Show - No Error

    [Test]
    public async Task Show_NoError_ReturnsZero()
    {
        var result = _writer.Show();

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task Show_NoError_DisplaysTitle()
    {
        _writer.Show();

        await Assert.That(_console.Output).Contains("DraftSpec - RSpec-style testing for .NET");
    }

    [Test]
    public async Task Show_NoError_DisplaysUsageSection()
    {
        _writer.Show();

        await Assert.That(_console.Output).Contains("Usage:");
        await Assert.That(_console.Output).Contains("draftspec run");
        await Assert.That(_console.Output).Contains("draftspec watch");
        await Assert.That(_console.Output).Contains("draftspec list");
        await Assert.That(_console.Output).Contains("draftspec init");
        await Assert.That(_console.Output).Contains("draftspec new");
    }

    [Test]
    public async Task Show_NoError_DisplaysOptionsSection()
    {
        _writer.Show();

        await Assert.That(_console.Output).Contains("Options:");
        await Assert.That(_console.Output).Contains("--format");
        await Assert.That(_console.Output).Contains("--output");
        await Assert.That(_console.Output).Contains("--parallel");
    }

    [Test]
    public async Task Show_NoError_DisplaysCoverageOptions()
    {
        _writer.Show();

        await Assert.That(_console.Output).Contains("Coverage Options:");
        await Assert.That(_console.Output).Contains("--coverage");
        await Assert.That(_console.Output).Contains("--coverage-output");
        await Assert.That(_console.Output).Contains("--coverage-format");
    }

    [Test]
    public async Task Show_NoError_DisplaysListOptions()
    {
        _writer.Show();

        await Assert.That(_console.Output).Contains("List Options:");
        await Assert.That(_console.Output).Contains("--list-format");
        await Assert.That(_console.Output).Contains("--show-line-numbers");
        await Assert.That(_console.Output).Contains("--focused-only");
    }

    [Test]
    public async Task Show_NoError_DisplaysWatchOptions()
    {
        _writer.Show();

        await Assert.That(_console.Output).Contains("Watch Options:");
        await Assert.That(_console.Output).Contains("--incremental");
    }

    [Test]
    public async Task Show_NoError_DisplaysExamples()
    {
        _writer.Show();

        await Assert.That(_console.Output).Contains("Examples:");
        await Assert.That(_console.Output).Contains("draftspec init");
        await Assert.That(_console.Output).Contains("draftspec run ./specs");
    }

    [Test]
    public async Task Show_NoError_NoErrorOutput()
    {
        _writer.Show();

        // No error messages should be written when there's no error
        await Assert.That(_console.Errors).DoesNotContain("Error:");
    }

    #endregion

    #region Show - With Error

    [Test]
    public async Task Show_WithError_ReturnsOne()
    {
        var result = _writer.Show("Something went wrong");

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task Show_WithError_DisplaysErrorMessage()
    {
        _writer.Show("Invalid command");

        await Assert.That(_console.Errors).Contains("Error: Invalid command");
    }

    [Test]
    public async Task Show_WithError_StillDisplaysUsage()
    {
        _writer.Show("Some error");

        await Assert.That(_console.Output).Contains("Usage:");
        await Assert.That(_console.Output).Contains("Examples:");
    }

    [Test]
    public async Task Show_WithError_ErrorAppearsBeforeUsage()
    {
        _writer.Show("First error");

        var output = _console.Output;
        var errorIndex = output.IndexOf("Error:", StringComparison.Ordinal);
        var usageIndex = output.IndexOf("Usage:", StringComparison.Ordinal);

        await Assert.That(errorIndex).IsLessThan(usageIndex);
    }

    [Test]
    public async Task Show_WithUnknownCommand_DisplaysCommandInError()
    {
        _writer.Show("Unknown command: foobar");

        await Assert.That(_console.Errors).Contains("Unknown command: foobar");
    }

    #endregion

    #region Path Documentation

    [Test]
    public async Task Show_DocumentsPathOptions()
    {
        _writer.Show();

        await Assert.That(_console.Output).Contains("Path can be:");
        await Assert.That(_console.Output).Contains("A directory");
        await Assert.That(_console.Output).Contains(".spec.csx");
    }

    #endregion
}
