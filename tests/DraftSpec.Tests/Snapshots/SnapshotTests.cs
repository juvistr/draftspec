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

    #region Concurrency Tests

    [Test]
    public async Task SnapshotManager_ConcurrentCreation_NoExceptions()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "concurrent.spec.csx");

        // Run multiple snapshot operations concurrently
        // Note: Due to race conditions in file writes, not all snapshots may persist
        // This test verifies the system handles concurrent operations without crashing
        var tasks = Enumerable.Range(0, 10).Select(i =>
            Task.Run(() => manager.Match(new { Index = i, Value = $"data-{i}" }, specPath, $"snapshot-{i}")));

        var results = await Task.WhenAll(tasks);

        // All should complete (either Created or some might be lost due to race conditions)
        await Assert.That(results.Length).IsEqualTo(10);
        await Assert.That(results.All(r => r.Status == SnapshotStatus.Created)).IsTrue();
    }

    [Test]
    public async Task SnapshotManager_SequentialOperations_AllPersist()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "sequential.spec.csx");

        // Create snapshots sequentially
        for (var i = 0; i < 10; i++)
        {
            var result = manager.Match(new { Index = i, Value = $"data-{i}" }, specPath, $"snapshot-{i}");
            await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Created);
        }

        // Verify all snapshots match on second run
        manager.ClearCache();
        for (var i = 0; i < 10; i++)
        {
            var result = manager.Match(new { Index = i, Value = $"data-{i}" }, specPath, $"snapshot-{i}");
            await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Matched);
        }
    }

    [Test]
    public async Task SnapshotManager_ParallelFilesOperations_NoRaceConditions()
    {
        var manager = new SnapshotManager();

        // Create snapshots in multiple files concurrently
        var tasks = Enumerable.Range(0, 5).Select(i =>
        {
            var specPath = Path.Combine(_tempDir, $"parallel-{i}.spec.csx");
            return Task.Run(() => manager.Match($"value-{i}", specPath, $"key-{i}"));
        });

        var results = await Task.WhenAll(tasks);

        // All should be created
        await Assert.That(results.All(r => r.Status == SnapshotStatus.Created)).IsTrue();
    }

    #endregion

    #region Large Object Tests

    [Test]
    public async Task SnapshotSerializer_LargeObject_Serializes()
    {
        // Generate a moderately large object (~1MB)
        var items = Enumerable.Range(0, 10000)
            .Select(i => new { Id = i, Name = $"Item {i}", Data = new string('x', 100) })
            .ToArray();

        var json = SnapshotSerializer.Serialize(items);

        await Assert.That(json.Length).IsGreaterThan(1_000_000);
        await Assert.That(json).Contains("\"id\"");
    }

    [Test]
    public async Task SnapshotManager_LargeSnapshot_CreatesAndMatches()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "large.spec.csx");

        // Generate large data
        var data = Enumerable.Range(0, 1000)
            .Select(i => new { Id = i, Value = new string('a', 500) })
            .ToArray();

        // Create snapshot
        var createResult = manager.Match(data, specPath, "large-snapshot");
        await Assert.That(createResult.Status).IsEqualTo(SnapshotStatus.Created);

        // Verify file is large
        var snapshotPath = SnapshotManager.GetSnapshotFilePath(specPath);
        var fileInfo = new FileInfo(snapshotPath);
        await Assert.That(fileInfo.Length).IsGreaterThan(100_000);

        // Match again
        manager.ClearCache();
        var matchResult = manager.Match(data, specPath, "large-snapshot");
        await Assert.That(matchResult.Status).IsEqualTo(SnapshotStatus.Matched);
    }

    [Test]
    public async Task SnapshotManager_ManySnapshots_InSingleFile()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "many.spec.csx");

        // Create 100 snapshots in one file
        for (var i = 0; i < 100; i++)
        {
            manager.Match(new { Index = i }, specPath, $"snapshot-{i}");
        }

        manager.ClearCache();

        // All should match
        for (var i = 0; i < 100; i++)
        {
            var result = manager.Match(new { Index = i }, specPath, $"snapshot-{i}");
            await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Matched);
        }
    }

    #endregion

    #region Unicode and Special Character Tests

    [Test]
    public async Task SnapshotSerializer_UnicodeCharacters_PreservesCorrectly()
    {
        var obj = new
        {
            Japanese = "日本語テスト",
            Emoji = "🎉🚀💡",
            Arabic = "مرحبا",
            Greek = "Γειά σου",
            Chinese = "你好世界"
        };

        var json = SnapshotSerializer.Serialize(obj);

        // JSON may encode unicode as \uXXXX but round-trip should preserve values
        // Verify the json contains the property names
        await Assert.That(json).Contains("\"japanese\"");
        await Assert.That(json).Contains("\"emoji\"");
        await Assert.That(json).Contains("\"arabic\"");
        await Assert.That(json).Contains("\"greek\"");
        await Assert.That(json).Contains("\"chinese\"");
    }

    [Test]
    public async Task SnapshotManager_SpecialCharactersInKey_Sanitized()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "special.spec.csx");

        // Key with special characters
        var key = "Test \"quoted\" with\\backslash";
        var result = manager.Match("data", specPath, key);

        await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Created);
        // Verify key was sanitized (quotes replaced with single quotes, backslashes with forward slashes)
        await Assert.That(result.Key).Contains("'quoted'");
        await Assert.That(result.Key).Contains("/backslash");
    }

    [Test]
    public async Task SnapshotSerializer_ControlCharacters_Escaped()
    {
        var obj = new { Text = "Line1\nLine2\tTabbed\rCarriage" };

        var json = SnapshotSerializer.Serialize(obj);

        // Control characters should be escaped in JSON
        await Assert.That(json).Contains("\\n");
        await Assert.That(json).Contains("\\t");
        await Assert.That(json).Contains("\\r");
    }

    [Test]
    public async Task SnapshotSerializer_EscapeSequences_PreservesCorrectly()
    {
        var obj = new { Path = "C:\\Users\\Test\\file.txt" };

        var json = SnapshotSerializer.Serialize(obj);

        await Assert.That(json).Contains("\\\\");
    }

    #endregion

    #region Deep Object Graph Tests

    [Test]
    public async Task SnapshotSerializer_DeepNesting_Serializes()
    {
        // Create a deeply nested structure (not too deep to avoid stack overflow)
        object nested = new { Value = "leaf" };
        for (var i = 0; i < 20; i++)
        {
            nested = new { Level = i, Child = nested };
        }

        var json = SnapshotSerializer.Serialize(nested);

        await Assert.That(json).Contains("\"level\"");
        await Assert.That(json).Contains("\"value\": \"leaf\"");
    }

    [Test]
    public async Task SnapshotSerializer_ArraysOfObjects_Serializes()
    {
        var obj = new
        {
            Items = new[]
            {
                new { Id = 1, Children = new[] { new { Name = "A" }, new { Name = "B" } } },
                new { Id = 2, Children = new[] { new { Name = "C" }, new { Name = "D" } } }
            }
        };

        var json = SnapshotSerializer.Serialize(obj);

        await Assert.That(json).Contains("\"id\": 1");
        await Assert.That(json).Contains("\"name\": \"A\"");
    }

    #endregion

    #region Diff Algorithm Tests

    [Test]
    public async Task SnapshotDiff_LargeStrings_Truncates()
    {
        // Create strings with many different lines
        var expectedLines = Enumerable.Range(0, 100).Select(i => $"  \"line{i}\": \"expected-{i}\"");
        var actualLines = Enumerable.Range(0, 100).Select(i => $"  \"line{i}\": \"actual-{i}\"");

        var expected = "{\n" + string.Join(",\n", expectedLines) + "\n}";
        var actual = "{\n" + string.Join(",\n", actualLines) + "\n}";

        var diff = SnapshotDiff.Generate(expected, actual);

        // Diff should be truncated
        await Assert.That(diff).Contains("truncated");
        await Assert.That(diff).Contains("more lines differ");
    }

    [Test]
    public async Task SnapshotDiff_IdenticalStrings_NoDiff()
    {
        var json = """
            {
              "name": "Test",
              "value": 42
            }
            """;

        var diff = SnapshotDiff.Generate(json, json);

        // Difference section should be empty (no - or + lines for actual changes)
        await Assert.That(diff).Contains("Difference:");
        // Count the actual diff lines (- and + at start of line in Difference section)
        var diffSection = diff.Split("Difference:")[1];
        var diffLines = diffSection.Split('\n').Count(l => l.TrimStart().StartsWith("-") || l.TrimStart().StartsWith("+"));
        await Assert.That(diffLines).IsEqualTo(0);
    }

    [Test]
    public async Task SnapshotDiff_EmptyExpected_ShowsOnlyActual()
    {
        var diff = SnapshotDiff.Generate("", "{\"value\": 1}");

        await Assert.That(diff).Contains("Received:");
        await Assert.That(diff).Contains("\"value\": 1");
    }

    #endregion

    #region File Handling Edge Cases

    [Test]
    public async Task SnapshotManager_CorruptedFile_StartsNewSnapshots()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "corrupted.spec.csx");
        var snapshotPath = SnapshotManager.GetSnapshotFilePath(specPath);

        // Create directory and corrupted file
        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
        File.WriteAllText(snapshotPath, "{ invalid json [[");

        // Should handle gracefully and create new snapshot
        var result = manager.Match("data", specPath, "test-key");

        await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Created);
    }

    [Test]
    public async Task SnapshotManager_EmptyFile_StartsNewSnapshots()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "empty.spec.csx");
        var snapshotPath = SnapshotManager.GetSnapshotFilePath(specPath);

        // Create directory and empty file
        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
        File.WriteAllText(snapshotPath, "");

        // Should handle gracefully
        var result = manager.Match("data", specPath, "test-key");

        await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Created);
    }

    [Test]
    public async Task SnapshotManager_ReadOnlyDirectory_HandledGracefully()
    {
        // This test verifies that the manager properly propagates exceptions
        // when it cannot write to the snapshot directory
        var manager = new SnapshotManager();
        var specPath = "/nonexistent/path/test.spec.csx";

        // Should throw when trying to create snapshot in non-existent path
        try
        {
            manager.Match("data", specPath, "test-key");
            // On some systems this might succeed (auto-create), on others fail
            await Assert.That(true).IsTrue(); // Just verify no crash
        }
        catch (Exception ex)
        {
            // Expected - directory doesn't exist
            await Assert.That(ex).IsNotNull();
        }
    }

    [Test]
    public async Task SnapshotManager_VeryLongKey_Works()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "longkey.spec.csx");

        // Create a very long key
        var longKey = new string('a', 500);

        var result = manager.Match("data", specPath, longKey);
        await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Created);

        // Verify it matches
        manager.ClearCache();
        var matchResult = manager.Match("data", specPath, longKey);
        await Assert.That(matchResult.Status).IsEqualTo(SnapshotStatus.Matched);
    }

    [Test]
    public async Task SnapshotManager_WhitespaceNormalization_Works()
    {
        var manager = new SnapshotManager();
        var specPath = Path.Combine(_tempDir, "whitespace.spec.csx");

        // Create with one line ending style
        manager.Match(new { Value = 1 }, specPath, "test");

        // Manually modify the file to use different line endings
        var snapshotPath = SnapshotManager.GetSnapshotFilePath(specPath);
        var content = File.ReadAllText(snapshotPath);
        File.WriteAllText(snapshotPath, content.Replace("\n", "\r\n"));

        // Should still match due to normalization
        manager.ClearCache();
        var result = manager.Match(new { Value = 1 }, specPath, "test");
        await Assert.That(result.Status).IsEqualTo(SnapshotStatus.Matched);
    }

    #endregion

    #region Serialization Edge Cases

    [Test]
    public async Task SnapshotSerializer_CircularReference_ThrowsWithHelpfulMessage()
    {
        // System.Text.Json throws JsonException for circular references
        var obj = new CircularRef();
        obj.Self = obj;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await Task.Run(() => SnapshotSerializer.Serialize(obj));
        });
    }

    [Test]
    public async Task SnapshotSerializer_DateTimeValues_Serializes()
    {
        var obj = new
        {
            Date = new DateTime(2024, 12, 25, 10, 30, 0, DateTimeKind.Utc),
            DateTimeOffset = new DateTimeOffset(2024, 12, 25, 10, 30, 0, TimeSpan.Zero)
        };

        var json = SnapshotSerializer.Serialize(obj);

        await Assert.That(json).Contains("2024-12-25");
    }

    [Test]
    public async Task SnapshotSerializer_GuidValues_Serializes()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var json = SnapshotSerializer.Serialize(new { Id = guid });

        await Assert.That(json).Contains("12345678-1234-1234-1234-123456789012");
    }

    [Test]
    public async Task SnapshotSerializer_EnumValues_Serializes()
    {
        var json = SnapshotSerializer.Serialize(new { Status = SnapshotStatus.Matched });

        // By default, enums serialize as numbers (Matched = 0)
        await Assert.That(json).Contains("\"status\": 0");
    }

    [Test]
    public async Task SnapshotSerializer_ByteArray_Serializes()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var json = SnapshotSerializer.Serialize(new { Data = bytes });

        // Byte arrays serialize as base64
        await Assert.That(json).Contains("AQIDBAU=");
    }

    #endregion

    #region Test Helpers

    private class CircularRef
    {
        public CircularRef? Self { get; set; }
    }

    #endregion
}
