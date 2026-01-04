using DraftSpec.Cli;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for SystemPathComparer to verify OS-specific path comparison behavior.
/// </summary>
public class SystemPathComparerTests
{
    [Test]
    public async Task Comparison_OnWindows_ReturnsOrdinalIgnoreCase()
    {
        var os = new MockOperatingSystem().WithWindows();
        var comparer = new SystemPathComparer(os);

        await Assert.That(comparer.Comparison).IsEqualTo(StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task Comparison_OnMacOS_ReturnsOrdinal()
    {
        var os = new MockOperatingSystem().WithMacOS();
        var comparer = new SystemPathComparer(os);

        await Assert.That(comparer.Comparison).IsEqualTo(StringComparison.Ordinal);
    }

    [Test]
    public async Task Comparison_OnLinux_ReturnsOrdinal()
    {
        var os = new MockOperatingSystem(); // Neither Windows nor macOS
        var comparer = new SystemPathComparer(os);

        await Assert.That(comparer.Comparison).IsEqualTo(StringComparison.Ordinal);
    }

    [Test]
    public async Task Comparer_OnWindows_ReturnsOrdinalIgnoreCase()
    {
        var os = new MockOperatingSystem().WithWindows();
        var comparer = new SystemPathComparer(os);

        await Assert.That(comparer.Comparer).IsEqualTo(StringComparer.OrdinalIgnoreCase);
    }

    [Test]
    public async Task Comparer_OnMacOS_ReturnsOrdinal()
    {
        var os = new MockOperatingSystem().WithMacOS();
        var comparer = new SystemPathComparer(os);

        await Assert.That(comparer.Comparer).IsEqualTo(StringComparer.Ordinal);
    }

    [Test]
    public async Task Comparer_OnLinux_ReturnsOrdinal()
    {
        var os = new MockOperatingSystem(); // Neither Windows nor macOS
        var comparer = new SystemPathComparer(os);

        await Assert.That(comparer.Comparer).IsEqualTo(StringComparer.Ordinal);
    }

    [Test]
    public async Task Comparer_OnWindows_MatchesCaseInsensitive()
    {
        var os = new MockOperatingSystem().WithWindows();
        var comparer = new SystemPathComparer(os);

        var result = comparer.Comparer.Equals("File.txt", "FILE.TXT");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Comparer_OnLinux_DoesNotMatchCaseInsensitive()
    {
        var os = new MockOperatingSystem(); // Linux
        var comparer = new SystemPathComparer(os);

        var result = comparer.Comparer.Equals("File.txt", "FILE.TXT");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Comparison_OnWindows_MatchesCaseInsensitive()
    {
        var os = new MockOperatingSystem().WithWindows();
        var comparer = new SystemPathComparer(os);

        var result = string.Equals("File.txt", "FILE.TXT", comparer.Comparison);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Comparison_OnLinux_DoesNotMatchCaseInsensitive()
    {
        var os = new MockOperatingSystem(); // Linux
        var comparer = new SystemPathComparer(os);

        var result = string.Equals("File.txt", "FILE.TXT", comparer.Comparison);

        await Assert.That(result).IsFalse();
    }
}
