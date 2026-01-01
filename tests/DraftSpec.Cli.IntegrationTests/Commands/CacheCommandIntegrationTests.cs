using DraftSpec.Cli.IntegrationTests.Infrastructure;

namespace DraftSpec.Cli.IntegrationTests.Commands;

/// <summary>
/// Integration tests for the cache command (stats and clear subcommands).
/// Tests run the actual CLI as a subprocess.
/// </summary>
[NotInParallel("CacheCommand")]
public class CacheCommandIntegrationTests : IntegrationTestBase
{
    #region Stats Subcommand

    [Test]
    public async Task Stats_EmptyCache_ShowsZeroCounts()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "cache", "stats");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Stats command should succeed");
        await Assert.That(result.Output).Contains("Entries: 0")
            .Because("Empty cache should show 0 entries");
    }

    [Test]
    public async Task Stats_AfterRunningSpecs_ShowsCacheEntries()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        // First, run specs to populate the cache
        await RunCliInDirectoryAsync(specDir, "run", ".");

        // Then check cache stats
        var result = await RunCliInDirectoryAsync(specDir, "cache", "stats");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Stats command should succeed");
        await Assert.That(result.Output).Contains("Cache Statistics:")
            .Because("Should show cache statistics header");
        await Assert.That(result.Output).Contains(".draftspec")
            .Because("Should show cache location");
    }

    [Test]
    public async Task Stats_ShowsCacheLocation()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "cache", "stats");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("Location:")
            .Because("Should display cache location");
        await Assert.That(result.Output).Contains(".draftspec")
            .Because("Cache location should include .draftspec directory");
    }

    [Test]
    public async Task Stats_ShowsBothCacheTypes()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "cache", "stats");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("Script Compilation Cache:")
            .Because("Should show script compilation cache section");
        await Assert.That(result.Output).Contains("Parse Result Cache:")
            .Because("Should show parse result cache section");
    }

    #endregion

    #region Clear Subcommand

    [Test]
    public async Task Clear_EmptyCache_ShowsAlreadyEmpty()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "cache", "clear");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Clear command should succeed even on empty cache");
        await Assert.That(result.Output).Contains("already empty")
            .Because("Should indicate cache was already empty");
    }

    [Test]
    public async Task Clear_WithEntries_ClearsAndShowsCount()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        // First, run specs to populate the cache
        await RunCliInDirectoryAsync(specDir, "run", ".");

        // Then clear the cache
        var result = await RunCliInDirectoryAsync(specDir, "cache", "clear");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Clear command should succeed");
        await Assert.That(result.Output).Contains("Cleared")
            .Because("Should indicate entries were cleared");
    }

    [Test]
    public async Task Clear_ActuallyDeletesFiles()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        // Run specs to populate cache
        await RunCliInDirectoryAsync(specDir, "run", ".");

        // Clear the cache
        await RunCliInDirectoryAsync(specDir, "cache", "clear");

        // Verify stats show empty
        var result = await RunCliInDirectoryAsync(specDir, "cache", "stats");

        await Assert.That(result.Output).Contains("Entries: 0")
            .Because("Cache should be empty after clear");
    }

    #endregion

    #region Error Handling

    [Test]
    public async Task UnknownSubcommand_ReturnsExitCodeOne()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "cache", "invalid");

        await Assert.That(result.ExitCode).IsEqualTo(1)
            .Because("Unknown subcommand should return exit code 1");
        await Assert.That(result.Output).Contains("Unknown cache subcommand")
            .Because("Should show error message for unknown subcommand");
    }

    [Test]
    public async Task UnknownSubcommand_ShowsUsage()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "cache", "delete");

        await Assert.That(result.ExitCode).IsEqualTo(1);
        await Assert.That(result.Output).Contains("stats")
            .Because("Should show available subcommands");
        await Assert.That(result.Output).Contains("clear")
            .Because("Should show available subcommands");
    }

    [Test]
    public async Task Stats_CaseInsensitive_AcceptsUppercase()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "cache", "STATS");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Subcommand should be case-insensitive");
        await Assert.That(result.Output).Contains("Cache Statistics:")
            .Because("Should show cache statistics");
    }

    [Test]
    public async Task Clear_CaseInsensitive_AcceptsUppercase()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "cache", "CLEAR");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Subcommand should be case-insensitive");
    }

    #endregion
}
