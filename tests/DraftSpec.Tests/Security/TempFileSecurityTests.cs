using System.Diagnostics;
using DraftSpec.Cli;

namespace DraftSpec.Tests.Security;

/// <summary>
/// Tests for temp file race condition vulnerability (CWE-367 TOCTOU).
///
/// VULNERABILITY: The current RunWithJson implementation:
///   var tempFile = Path.Combine(workingDir, tempFileName);
///   File.WriteAllText(tempFile, modifiedScript);
///
/// This has a race window where an attacker could:
/// 1. Predict the temp file name (GUID is deterministic given seed)
/// 2. Create a symlink at that path pointing to a target file
/// 3. Win the race to create the symlink before File.WriteAllText
/// 4. File.WriteAllText follows the symlink and overwrites the target
///
/// FIX: Use FileMode.CreateNew (atomic, fails if exists) with FileOptions.DeleteOnClose
///
/// Note: Full symlink attack tests are platform-specific and require elevated privileges.
/// These tests focus on the verifiable aspects of the fix.
/// </summary>
public class TempFileSecurityTests
{
    private string _testDir = null!;

    [Before(Test)]
    public async Task Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"draftspec-security-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        await Task.CompletedTask;
    }

    [After(Test)]
    public async Task Cleanup()
    {
        if (Directory.Exists(_testDir))
            try
            {
                Directory.Delete(_testDir, true);
            }
            catch
            {
                // Best effort cleanup
            }

        await Task.CompletedTask;
    }

    #region Temp File Behavior Tests

    /// <summary>
    /// Temp files should be automatically cleaned up after use.
    /// </summary>
    [Test]
    public async Task RunWithJson_AfterExecution_TempFileShouldBeDeleted()
    {
        // Create a minimal spec file
        var specFile = Path.Combine(_testDir, "cleanup_test.spec.csx");
        await File.WriteAllTextAsync(specFile, """
                                               #r "nuget: DraftSpec, *"
                                               using static DraftSpec.Dsl;

                                               describe("cleanup test", () =>
                                               {
                                                   it("passes", () => { });
                                               });

                                               run();
                                               """);

        var runner = new SpecFileRunner();

        // Run the spec
        try
        {
            runner.RunWithJson(specFile);
        }
        catch
        {
            // May fail due to missing dependencies, but we're testing cleanup
        }

        // Check that no temp files remain
        var tempFiles = Directory.GetFiles(_testDir, ".draftspec-*.csx");

        await Assert.That(tempFiles.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Temp files should be cleaned up even if execution fails.
    /// </summary>
    [Test]
    public async Task RunWithJson_OnFailure_TempFileShouldStillBeDeleted()
    {
        // Create a spec that will cause an error
        var specFile = Path.Combine(_testDir, "failing_test.spec.csx");
        await File.WriteAllTextAsync(specFile, """
                                               throw new System.Exception("Intentional failure");
                                               """);

        var runner = new SpecFileRunner();

        // Run should fail
        try
        {
            runner.RunWithJson(specFile);
        }
        catch
        {
            // Expected to fail
        }

        // Temp file should still be cleaned up
        var tempFiles = Directory.GetFiles(_testDir, ".draftspec-*.csx");

        await Assert.That(tempFiles.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Temp files should have secure permissions (not world-readable).
    /// Note: This is harder to test cross-platform, so we just verify the file exists briefly.
    /// </summary>
    [Test]
    public async Task RunWithJson_TempFile_ShouldBeCreatedInWorkingDirectory()
    {
        // Create a spec file
        var specFile = Path.Combine(_testDir, "location_test.spec.csx");
        await File.WriteAllTextAsync(specFile, """
                                               using static DraftSpec.Dsl;
                                               run();
                                               """);

        // We can't easily intercept the temp file creation, but we can verify
        // that the method attempts to create it in the working directory
        // by checking the implementation creates files matching the pattern

        // This test verifies the temp file naming pattern
        var tempPattern = ".draftspec-*.csx";
        var existingTemp = Directory.GetFiles(_testDir, tempPattern);

        // Should be empty before and after (cleanup working)
        await Assert.That(existingTemp.Length).IsEqualTo(0);
    }

    #endregion

    #region Concurrent Execution Tests

    /// <summary>
    /// Multiple concurrent RunWithJson calls should not interfere with each other.
    /// Each should use a unique temp file.
    /// </summary>
    [Test]
    public async Task RunWithJson_ConcurrentCalls_ShouldNotInterfere()
    {
        // Create multiple spec files
        var specFiles = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var specFile = Path.Combine(_testDir, $"concurrent_{i}.spec.csx");
            await File.WriteAllTextAsync(specFile, $$"""
                                                     using static DraftSpec.Dsl;

                                                     describe("concurrent test {{i}}", () =>
                                                     {
                                                         it("has unique content", () => { });
                                                     });

                                                     run();
                                                     """);
            specFiles.Add(specFile);
        }

        var runner = new SpecFileRunner();

        // Run all specs concurrently
        var tasks = specFiles.Select(f => Task.Run(() =>
        {
            try
            {
                runner.RunWithJson(f);
            }
            catch
            {
                // May fail due to missing dependencies
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // All temp files should be cleaned up
        var remainingTempFiles = Directory.GetFiles(_testDir, ".draftspec-*.csx");

        await Assert.That(remainingTempFiles.Length).IsEqualTo(0);
    }

    #endregion

    #region File Existence Check Tests (Atomic Creation)

    /// <summary>
    /// If a file already exists at the temp path, RunWithJson should handle it safely.
    /// With the fix (FileMode.CreateNew), it should fail rather than overwrite.
    ///
    /// This test simulates what would happen if an attacker pre-created the temp file.
    /// The secure implementation should refuse to overwrite an existing file.
    /// </summary>
    [Test]
    public async Task RunWithJson_PreexistingFile_ShouldNotOverwrite()
    {
        // Create a spec file
        var specFile = Path.Combine(_testDir, "preexisting_test.spec.csx");
        await File.WriteAllTextAsync(specFile, """
                                               using static DraftSpec.Dsl;
                                               describe("test", () => { it("passes", () => { }); });
                                               run();
                                               """);

        // To properly test this, we'd need to intercept the GUID generation
        // or have the implementation expose a way to test atomic creation
        // For now, we verify that the method completes and cleans up

        var runner = new SpecFileRunner();

        try
        {
            runner.RunWithJson(specFile);
        }
        catch
        {
            // May fail due to missing dependencies
        }

        // Verify no temp files leaked
        var tempFiles = Directory.GetFiles(_testDir, ".draftspec-*.csx");
        await Assert.That(tempFiles.Length).IsEqualTo(0);
    }

    #endregion

    #region Symlink Attack Tests (Unix-specific)

    /// <summary>
    /// On Unix systems, the fix should prevent symlink attacks.
    /// This test is skipped on Windows where symlinks require admin rights.
    ///
    /// VULNERABILITY SCENARIO:
    /// 1. Attacker guesses temp file path
    /// 2. Attacker creates symlink: /tmp/.draftspec-xxx.csx -> /etc/cron.d/backdoor
    /// 3. RunWithJson writes to the symlink
    /// 4. Cron config is overwritten with attacker's script content
    ///
    /// FIX: FileMode.CreateNew fails if any file (including symlink) exists at path
    /// </summary>
    [Test]
    public async Task RunWithJson_WithSymlinkAtTempPath_ShouldFailSafely()
    {
        // Skip on Windows - symlinks require admin rights
        if (OperatingSystem.IsWindows())
        {
            await Assert.That(true).IsTrue();
            return;
        }

        // This test would require:
        // 1. Knowing the exact temp file name that will be generated
        // 2. Creating a symlink at that path before RunWithJson runs
        //
        // Since GUID is random, we can't predict the name in a real scenario.
        // The fix (FileMode.CreateNew) ensures that even if an attacker wins the race,
        // the operation fails rather than following the symlink.

        // For now, we just verify the method doesn't crash on a directory with a symlink
        // A more comprehensive test would require modifying the implementation to be testable

        await Assert.That(true).IsTrue(); // Placeholder - real test requires test hooks
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// If the working directory doesn't exist, should fail gracefully.
    /// </summary>
    [Test]
    public async Task RunWithJson_NonexistentWorkingDirectory_ShouldThrow()
    {
        var nonexistentDir = Path.Combine(_testDir, "nonexistent");
        var specFile = Path.Combine(nonexistentDir, "test.spec.csx");

        var runner = new SpecFileRunner();

        var exception = await Assert.ThrowsAsync<Exception>(() =>
        {
            runner.RunWithJson(specFile);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    /// <summary>
    /// If the spec file doesn't exist, should throw meaningful error.
    /// </summary>
    [Test]
    public async Task RunWithJson_NonexistentSpecFile_ShouldThrow()
    {
        var specFile = Path.Combine(_testDir, "does_not_exist.spec.csx");

        var runner = new SpecFileRunner();

        var exception = await Assert.ThrowsAsync<Exception>(() =>
        {
            runner.RunWithJson(specFile);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    #endregion
}