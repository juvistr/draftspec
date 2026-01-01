using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for CacheCommand which manages cached compilation and parse results.
/// </summary>
public class CacheCommandTests
{
    private string _tempDir = null!;
    private MockConsole _console = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_cache_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _console = new MockConsole();
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Stats Subcommand Tests

    [Test]
    public async Task ExecuteAsync_Stats_ShowsCacheStatistics()
    {
        var command = new CacheCommand(_console);
        var options = new CacheOptions { Subcommand = "stats", Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("Cache Statistics:");
        await Assert.That(_console.Output).Contains("Script Compilation Cache:");
        await Assert.That(_console.Output).Contains("Parse Result Cache:");
        await Assert.That(_console.Output).Contains("Entries:");
        await Assert.That(_console.Output).Contains("Size:");
        await Assert.That(_console.Output).Contains("Total:");
    }

    [Test]
    public async Task ExecuteAsync_Stats_ShowsLocationPath()
    {
        var command = new CacheCommand(_console);
        var options = new CacheOptions { Subcommand = "stats", Path = _tempDir };

        await command.ExecuteAsync(options);

        await Assert.That(_console.Output).Contains("Location:");
        await Assert.That(_console.Output).Contains(".draftspec");
        await Assert.That(_console.Output).Contains("cache");
    }

    [Test]
    public async Task ExecuteAsync_Stats_EmptyCache_ShowsZeroCounts()
    {
        var command = new CacheCommand(_console);
        var options = new CacheOptions { Subcommand = "stats", Path = _tempDir };

        await command.ExecuteAsync(options);

        // Both caches should show 0 entries for empty cache
        await Assert.That(_console.Output).Contains("Entries: 0");
        await Assert.That(_console.Output).Contains("Total: 0 entries");
    }

    #endregion

    #region Clear Subcommand Tests

    [Test]
    public async Task ExecuteAsync_Clear_EmptyCache_ShowsAlreadyEmpty()
    {
        var command = new CacheCommand(_console);
        var options = new CacheOptions { Subcommand = "clear", Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("Cache is already empty");
    }

    [Test]
    public async Task ExecuteAsync_Clear_WithCacheEntries_ClearsAndShowsCount()
    {
        // Create a cache directory with some files
        var cacheDir = Path.Combine(_tempDir, ".draftspec", "cache", "parsing");
        Directory.CreateDirectory(cacheDir);
        await File.WriteAllTextAsync(Path.Combine(cacheDir, "test.meta.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(cacheDir, "test.result.json"), "{}");

        var command = new CacheCommand(_console);
        var options = new CacheOptions { Subcommand = "clear", Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("Cleared");
        await Assert.That(_console.Output).Contains("cache entries");
    }

    #endregion

    #region Unknown Subcommand Tests

    [Test]
    public async Task ExecuteAsync_UnknownSubcommand_ShowsError()
    {
        var command = new CacheCommand(_console);
        var options = new CacheOptions { Subcommand = "unknown", Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Output).Contains("Unknown cache subcommand: unknown");
    }

    [Test]
    public async Task ExecuteAsync_UnknownSubcommand_ShowsUsage()
    {
        var command = new CacheCommand(_console);
        var options = new CacheOptions { Subcommand = "invalid", Path = _tempDir };

        await command.ExecuteAsync(options);

        await Assert.That(_console.Output).Contains("Usage:");
        await Assert.That(_console.Output).Contains("draftspec cache <subcommand>");
        await Assert.That(_console.Output).Contains("stats");
        await Assert.That(_console.Output).Contains("clear");
    }

    #endregion

    #region Size Formatting Tests

    [Test]
    public async Task ExecuteAsync_Stats_FormatsSmallSizes()
    {
        var command = new CacheCommand(_console);
        var options = new CacheOptions { Subcommand = "stats", Path = _tempDir };

        await command.ExecuteAsync(options);

        // Empty cache should show 0 B
        await Assert.That(_console.Output).Contains("0 B");
    }

    #endregion
}
