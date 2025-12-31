using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for SystemEnvironment wrapper.
/// </summary>
public class SystemEnvironmentTests
{
    [Test]
    public async Task CurrentDirectory_ReturnsNonEmpty()
    {
        var env = new SystemEnvironment();

        var dir = env.CurrentDirectory;

        await Assert.That(dir).IsNotNull();
        await Assert.That(dir.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task CurrentDirectory_MatchesDirectoryGetCurrentDirectory()
    {
        var env = new SystemEnvironment();

        var result = env.CurrentDirectory;

        await Assert.That(result).IsEqualTo(Directory.GetCurrentDirectory());
    }

    [Test]
    public async Task NewLine_ReturnsNonEmpty()
    {
        var env = new SystemEnvironment();

        var newLine = env.NewLine;

        await Assert.That(newLine).IsNotNull();
        await Assert.That(newLine.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task NewLine_MatchesEnvironmentNewLine()
    {
        var env = new SystemEnvironment();

        var result = env.NewLine;

        await Assert.That(result).IsEqualTo(Environment.NewLine);
    }
}
