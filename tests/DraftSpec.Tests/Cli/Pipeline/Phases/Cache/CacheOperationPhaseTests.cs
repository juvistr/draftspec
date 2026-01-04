using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Cache;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Cache;

/// <summary>
/// Tests for <see cref="CacheOperationPhase"/>.
/// </summary>
public class CacheOperationPhaseTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_cache_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
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
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "stats");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("Cache Statistics:");
    }

    [Test]
    public async Task ExecuteAsync_Stats_ShowsScriptCompilationCache()
    {
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "stats");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("Script Compilation Cache:");
        await Assert.That(console.Output).Contains("Entries:");
        await Assert.That(console.Output).Contains("Size:");
    }

    [Test]
    public async Task ExecuteAsync_Stats_ShowsParseResultCache()
    {
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "stats");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("Parse Result Cache:");
    }

    [Test]
    public async Task ExecuteAsync_Stats_ShowsLocation()
    {
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "stats");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("Location:");
        await Assert.That(console.Output).Contains(".draftspec");
    }

    [Test]
    public async Task ExecuteAsync_Stats_EmptyCache_ShowsZeroCounts()
    {
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "stats");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("Entries: 0");
        await Assert.That(console.Output).Contains("Total: 0 entries");
    }

    [Test]
    public async Task ExecuteAsync_Stats_EmptyCache_ShowsZeroBytes()
    {
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "stats");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("0 B");
    }

    #endregion

    #region Clear Subcommand Tests

    [Test]
    public async Task ExecuteAsync_Clear_EmptyCache_ShowsAlreadyEmpty()
    {
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "clear");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("Cache is already empty");
    }

    [Test]
    public async Task ExecuteAsync_Clear_WithCacheEntries_ClearsCache()
    {
        // Create cache directory with some test files
        var cacheDir = Path.Combine(_tempDir, ".draftspec", "cache", "parsing");
        Directory.CreateDirectory(cacheDir);
        await File.WriteAllTextAsync(Path.Combine(cacheDir, "test.meta.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(cacheDir, "test.result.json"), "{}");

        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "clear");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("Cleared");
    }

    [Test]
    public async Task ExecuteAsync_Clear_WithCacheEntries_ShowsEntryCount()
    {
        // Create cache directory with some test files
        var cacheDir = Path.Combine(_tempDir, ".draftspec", "cache", "parsing");
        Directory.CreateDirectory(cacheDir);
        await File.WriteAllTextAsync(Path.Combine(cacheDir, "test.meta.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(cacheDir, "test.result.json"), "{}");

        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "clear");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("cache entries");
    }

    #endregion

    #region Unknown Subcommand Tests

    [Test]
    public async Task ExecuteAsync_UnknownSubcommand_ReturnsErrorCode()
    {
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "unknown");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_UnknownSubcommand_ShowsError()
    {
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "invalid");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("Unknown cache subcommand: invalid");
    }

    [Test]
    public async Task ExecuteAsync_UnknownSubcommand_ShowsUsage()
    {
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "bad");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("Usage:");
        await Assert.That(console.Output).Contains("draftspec cache <subcommand>");
        await Assert.That(console.Output).Contains("stats");
        await Assert.That(console.Output).Contains("clear");
    }

    [Test]
    public async Task ExecuteAsync_EmptySubcommand_ShowsUsage()
    {
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("Usage:");
    }

    [Test]
    public async Task ExecuteAsync_NullSubcommand_TreatsAsEmpty()
    {
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        // Don't set CacheSubcommand - it will be null and coalesce to ""

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("Usage:");
    }

    #endregion

    #region Size Formatting Tests

    [Test]
    public async Task ExecuteAsync_Stats_WithKilobyteCache_FormatsAsKB()
    {
        // Create cache with .result.json file > 1KB but < 1MB
        // Cache size is calculated from .result.json files, not .meta.json
        var cacheDir = Path.Combine(_tempDir, ".draftspec", "cache", "parsing");
        Directory.CreateDirectory(cacheDir);
        // Create a ~2KB result file (size is calculated from result files)
        var content = new string('x', 2048);
        await File.WriteAllTextAsync(Path.Combine(cacheDir, "large.meta.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(cacheDir, "large.result.json"), content);

        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "stats");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("KB");
    }

    [Test]
    public async Task ExecuteAsync_Stats_WithMegabyteCache_FormatsAsMB()
    {
        // Create cache with .result.json file > 1MB
        // Cache size is calculated from .result.json files, not .meta.json
        var cacheDir = Path.Combine(_tempDir, ".draftspec", "cache", "parsing");
        Directory.CreateDirectory(cacheDir);
        // Create a ~1.5MB result file
        var content = new string('x', 1024 * 1024 + 512 * 1024);
        await File.WriteAllTextAsync(Path.Combine(cacheDir, "huge.meta.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(cacheDir, "huge.result.json"), content);

        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "stats");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains("MB");
    }

    [Test]
    public async Task ExecuteAsync_Stats_WithSmallCache_FormatsAsBytes()
    {
        // Create cache with small files (< 1KB)
        var cacheDir = Path.Combine(_tempDir, ".draftspec", "cache", "parsing");
        Directory.CreateDirectory(cacheDir);
        await File.WriteAllTextAsync(Path.Combine(cacheDir, "small.meta.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(cacheDir, "small.result.json"), "{}");

        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = CreateContext(console, _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "stats");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        // Should show bytes (not KB or MB) for small files
        // The total line should contain " B" but not "KB" or "MB"
        await Assert.That(console.Output).Contains(" B");
    }

    #endregion

    #region Missing ProjectPath Tests

    [Test]
    public async Task ExecuteAsync_NoProjectPath_ReturnsError()
    {
        var console = new MockConsole();
        var phase = new CacheOperationPhase();
        var context = new CommandContext
        {
            Path = ".",
            Console = console,
            FileSystem = new MockFileSystem()
        };
        // Don't set ProjectPath - simulating PathResolutionPhase not being run

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Output).Contains("ProjectPath not set");
    }

    #endregion

    #region Pipeline Propagation Tests

    [Test]
    public async Task ExecuteAsync_Stats_CallsPipeline()
    {
        var phase = new CacheOperationPhase();
        var context = CreateContext(new MockConsole(), _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "stats");
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_Clear_CallsPipeline()
    {
        var phase = new CacheOperationPhase();
        var context = CreateContext(new MockConsole(), _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "clear");
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_UnknownSubcommand_DoesNotCallPipeline()
    {
        var phase = new CacheOperationPhase();
        var context = CreateContext(new MockConsole(), _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "unknown");
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineResult()
    {
        var phase = new CacheOperationPhase();
        var context = CreateContext(new MockConsole(), _tempDir);
        context.Set(ContextKeys.CacheSubcommand, "stats");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    #endregion

    #region Helper Methods

    private static CommandContext CreateContext(MockConsole console, string projectPath)
    {
        var context = new CommandContext
        {
            Path = ".",
            Console = console,
            FileSystem = new MockFileSystem()
        };
        context.Set(ContextKeys.ProjectPath, projectPath);
        return context;
    }

    #endregion
}
