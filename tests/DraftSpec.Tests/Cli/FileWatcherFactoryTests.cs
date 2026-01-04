using DraftSpec.Cli;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for FileWatcherFactory.
/// </summary>
public class FileWatcherFactoryTests
{
    private static FileWatcherFactory CreateFactory() => new(new MockOperatingSystem());

    [Test]
    public async Task Create_ReturnsFileWatcher()
    {
        var factory = CreateFactory();
        var tempDir = Path.GetTempPath();

        using var watcher = factory.Create(tempDir);

        await Assert.That(watcher).IsNotNull();
        await Assert.That(watcher).IsAssignableTo<IFileWatcher>();
    }

    [Test]
    public async Task Create_WithDebounceMs_ReturnsFileWatcher()
    {
        var factory = CreateFactory();
        var tempDir = Path.GetTempPath();

        using var watcher = factory.Create(tempDir, debounceMs: 500);

        await Assert.That(watcher).IsNotNull();
    }

    [Test]
    public async Task Create_ReturnsNewInstanceEachTime()
    {
        var factory = CreateFactory();
        var tempDir = Path.GetTempPath();

        using var watcher1 = factory.Create(tempDir);
        using var watcher2 = factory.Create(tempDir);

        await Assert.That(watcher1).IsNotSameReferenceAs(watcher2);
    }

    [Test]
    public async Task Create_WithDifferentPaths_ReturnsDistinctWatchers()
    {
        var factory = CreateFactory();
        var tempDir1 = Path.Combine(Path.GetTempPath(), $"watcher_test_{Guid.NewGuid():N}");
        var tempDir2 = Path.Combine(Path.GetTempPath(), $"watcher_test_{Guid.NewGuid():N}");

        Directory.CreateDirectory(tempDir1);
        Directory.CreateDirectory(tempDir2);

        try
        {
            using var watcher1 = factory.Create(tempDir1);
            using var watcher2 = factory.Create(tempDir2);

            await Assert.That(watcher1).IsNotSameReferenceAs(watcher2);
        }
        finally
        {
            if (Directory.Exists(tempDir1))
                Directory.Delete(tempDir1, true);
            if (Directory.Exists(tempDir2))
                Directory.Delete(tempDir2, true);
        }
    }
}
