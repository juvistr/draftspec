using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for SystemOperatingSystem to verify it correctly wraps System.OperatingSystem.
/// These tests verify the implementation works, but OS-specific behavior depends on the test environment.
/// </summary>
public class SystemOperatingSystemTests
{
    [Test]
    public async Task IsWindows_ReturnsSystemValue()
    {
        var os = new SystemOperatingSystem();

        var result = os.IsWindows;

        // Verify it matches the actual system value
        await Assert.That(result).IsEqualTo(OperatingSystem.IsWindows());
    }

    [Test]
    public async Task IsMacOS_ReturnsSystemValue()
    {
        var os = new SystemOperatingSystem();

        var result = os.IsMacOS;

        // Verify it matches the actual system value
        await Assert.That(result).IsEqualTo(OperatingSystem.IsMacOS());
    }

    [Test]
    public async Task OnlyOneOsPlatform_IsTrue()
    {
        var os = new SystemOperatingSystem();

        // At most one of these should be true (or none if running on Linux)
        var platformCount = (os.IsWindows ? 1 : 0) + (os.IsMacOS ? 1 : 0);

        await Assert.That(platformCount).IsLessThanOrEqualTo(1);
    }
}
