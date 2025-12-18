using DraftSpec.Providers;
using DraftSpec.Snapshots;

namespace DraftSpec.Tests.Snapshots;

/// <summary>
/// Tests for snapshot testing infrastructure.
/// </summary>
public class SnapshotTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec-snapshot-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); }
            catch { /* ignore cleanup errors */ }
        }
    }

    #region SnapshotManager Tests

    [Test]
    public async Task SnapshotManager_CreatesSnapshotOnFirstRun()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "test.spec.csx");

        var result = manager.Match(
            new { Name = "Test", Value = 42 },
            specPath,
            "Test snapshot");

        await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Created);

        // Verify file was created
        var snapshotPath = SnapshotManager.GetSnapshotFilePath(specPath);
        await Assert.That(File.Exists(snapshotPath)).IsTrue();
    }

    [Test]
    public async Task SnapshotManager_MatchesExistingSnapshot()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        var data = new { Name = "Test", Value = 42 };

        // Create snapshot
        manager.Match(data, specPath, "Test snapshot");
        manager.ClearCache();

        // Match again
        var result = manager.Match(data, specPath, "Test snapshot");

        await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Matched);
    }

    [Test]
    public async Task SnapshotManager_DetectsMismatch()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "test.spec.csx");

        // Create snapshot with original data
        manager.Match(new { Name = "Original" }, specPath, "Test snapshot");
        manager.ClearCache();

        // Try to match with different data
        var result = manager.Match(new { Name = "Changed" }, specPath, "Test snapshot");

        await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Mismatched);
        await Assert.That(result.Diff).IsNotNull();
        await Assert.That(result.Diff!).Contains("Original");
        await Assert.That(result.Diff!).Contains("Changed");
    }

    [Test]
    public async Task SnapshotManager_UpdatesInUpdateMode()
    {
        var env = new InMemoryEnvironmentProvider();
        env.SetEnvironmentVariable(SnapshotManager.UpdateEnvVar, "true");
        var manager = new SnapshotManager(env);
        var specPath = Path.Combine(_tempDir, "test.spec.csx");

        // Create initial snapshot
        manager.Match(new { Name = "Original" }, specPath, "Test snapshot");
        manager.ClearCache();

        // Update with new data
        var result = manager.Match(new { Name = "Updated" }, specPath, "Test snapshot");

        await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Updated);
    }

    [Test]
    public async Task SnapshotManager_ReturnsCreatedForNewSnapshotInUpdateMode()
    {
        var env = new InMemoryEnvironmentProvider();
        env.SetEnvironmentVariable(SnapshotManager.UpdateEnvVar, "true");
        var manager = new SnapshotManager(env);
        var specPath = Path.Combine(_tempDir, "test.spec.csx");

        var result = manager.Match(new { Name = "New" }, specPath, "New snapshot");

        // New snapshot creation is shown as "Created" not "Updated"
        await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Created);
    }

    [Test]
    public async Task SnapshotManager_HandlesMultipleSnapshotsInOneFile()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "test.spec.csx");

        manager.Match("First", specPath, "snapshot-1");
        manager.Match("Second", specPath, "snapshot-2");
        manager.Match("Third", specPath, "snapshot-3");

        manager.ClearCache();

        // All should match
        await Assert.That(manager.Match("First", specPath, "snapshot-1").Status)
            .IsEqualTo(SnapshotStatus.Matched);
        await Assert.That(manager.Match("Second", specPath, "snapshot-2").Status)
            .IsEqualTo(SnapshotStatus.Matched);
        await Assert.That(manager.Match("Third", specPath, "snapshot-3").Status)
            .IsEqualTo(SnapshotStatus.Matched);
    }

    [Test]
    public async Task SnapshotManager_UsesCustomName()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "test.spec.csx");

        var result = manager.Match("data", specPath, "default-key", "custom-name");

        await Assert.That(result.Key).IsEqualTo("custom-name");
    }

    [Test]
    public async Task SnapshotManager_GetSnapshotDir_CreatesCorrectPath()
    {
        var specPath = "/path/to/specs/test.spec.csx";
        var dir = SnapshotManager.GetSnapshotDir(specPath);

        await Assert.That(dir).IsEqualTo("/path/to/specs/__snapshots__");
    }

    [Test]
    public async Task SnapshotManager_GetSnapshotFilePath_CreatesCorrectPath()
    {
        var specPath = "/path/to/specs/test.spec.csx";
        var path = SnapshotManager.GetSnapshotFilePath(specPath);

        // GetFileNameWithoutExtension removes only .csx, leaving test.spec
        await Assert.That(path).IsEqualTo("/path/to/specs/__snapshots__/test.spec.snap.json");
    }

    #endregion

    #region SnapshotContext Tests

    [Test]
    public async Task SnapshotContext_DefaultsToNull()
    {
        await Assert.That(SnapshotContext.Current).IsNull();
    }

    [Test]
    public async Task SnapshotContext_CanBeSetAndRetrieved()
    {
        var info = new SnapshotInfo("Full description", ["Context"], "spec");

        SnapshotContext.Current = info;
        try
        {
            await Assert.That(SnapshotContext.Current).IsEqualTo(info);
        }
        finally
        {
            SnapshotContext.Current = null;
        }
    }

    #endregion

    #region SnapshotSerializer Tests

    [Test]
    public async Task SnapshotSerializer_SerializesComplexObjects()
    {
        var obj = new { Name = "Test", Items = new[] { 1, 2, 3 } };
        var json = SnapshotSerializer.Serialize(obj);

        await Assert.That(json).Contains("\"name\"");
        await Assert.That(json).Contains("\"items\"");
    }

    [Test]
    public async Task SnapshotSerializer_HandlesNullValues()
    {
        var json = SnapshotSerializer.Serialize<object?>(null);
        await Assert.That(json).IsEqualTo("null");
    }

    #endregion

    #region SnapshotDiff Tests

    [Test]
    public async Task SnapshotDiff_GeneratesDiff()
    {
        var expected = """
            {
              "name": "Original"
            }
            """;
        var actual = """
            {
              "name": "Changed"
            }
            """;

        var diff = SnapshotDiff.Generate(expected, actual);

        await Assert.That(diff).Contains("Original");
        await Assert.That(diff).Contains("Changed");
        await Assert.That(diff).Contains("-");
        await Assert.That(diff).Contains("+");
    }

    #endregion

    #region toMatchSnapshot Extension Tests

    [Test]
    public async Task ToMatchSnapshot_ThrowsOutsideItBlock()
    {
        SnapshotContext.Current = null;

        var expectation = new Expectation<string>("test", "expr");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await Task.Run(() => expectation.toMatchSnapshot());
        });
    }

    [Test]
    public async Task ToMatchSnapshot_WorksInsideContext()
    {
        // Set up context
        SnapshotContext.Current = new SnapshotInfo(
            "Test Context works correctly",
            ["Test Context"],
            "works correctly");

        var tempSpecPath = Path.Combine(_tempDir, "context-test.spec.csx");

        // Create a mock manager with update mode to create snapshot
        var env = new InMemoryEnvironmentProvider();
        env.SetEnvironmentVariable(SnapshotManager.UpdateEnvVar, "true");
        SnapshotExpectationExtensions.Manager = new SnapshotManager(env);

        try
        {
            var expectation = new Expectation<object>(new { Value = 42 }, "expr");

            // Should not throw - creates snapshot
            expectation.toMatchSnapshot(callerFilePath: tempSpecPath);
        }
        finally
        {
            SnapshotContext.Current = null;
            SnapshotExpectationExtensions.Manager = new SnapshotManager();
        }
    }

    #endregion
}
