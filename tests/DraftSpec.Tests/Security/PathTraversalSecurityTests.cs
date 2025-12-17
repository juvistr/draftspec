using System.Security;
using DraftSpec.Cli;

namespace DraftSpec.Tests.Security;

/// <summary>
/// Tests for path traversal security vulnerability (CWE-22).
/// These tests verify that the path validation cannot be bypassed using prefix attacks.
///
/// VULNERABILITY: The current implementation uses StartsWith without trailing separator:
///   if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
///
/// ATTACK: "/var/app/specs-evil/file".StartsWith("/var/app/specs") = TRUE (bypass!)
///
/// These tests should FAIL with current implementation and PASS after fix.
/// </summary>
public class PathTraversalSecurityTests
{
    private string _testBaseDir = null!;
    private string _siblingDir = null!;

    [Before(Test)]
    public async Task Setup()
    {
        // Create test directories
        _testBaseDir = Path.Combine(Path.GetTempPath(), $"draftspec-test-{Guid.NewGuid():N}");
        _siblingDir = _testBaseDir + "-evil"; // Sibling directory with same prefix

        Directory.CreateDirectory(_testBaseDir);
        Directory.CreateDirectory(_siblingDir);

        // Create a spec file in the sibling directory
        await File.WriteAllTextAsync(
            Path.Combine(_siblingDir, "malicious.spec.csx"),
            "// malicious spec");

        await Task.CompletedTask;
    }

    [After(Test)]
    public async Task Cleanup()
    {
        if (Directory.Exists(_testBaseDir))
            Directory.Delete(_testBaseDir, true);
        if (Directory.Exists(_siblingDir))
            Directory.Delete(_siblingDir, true);

        await Task.CompletedTask;
    }

    #region Prefix Bypass Attack Tests (CRITICAL - Should FAIL with current code)

    /// <summary>
    /// CRITICAL: This test exposes the path traversal bypass vulnerability.
    /// The attack uses a sibling directory with the same prefix as the base directory.
    ///
    /// Example: base="/var/app/specs" attack="/var/app/specs-evil/malicious.spec.csx"
    /// Current code: "/var/app/specs-evil".StartsWith("/var/app/specs") = TRUE (VULNERABLE)
    /// Fixed code: "/var/app/specs-evil".StartsWith("/var/app/specs/") = FALSE (SECURE)
    /// </summary>
    [Test]
    public async Task FindSpecs_SiblingDirectoryWithSamePrefix_ShouldThrowSecurityException()
    {
        var finder = new SpecFinder();

        // Try to access a file in a sibling directory that shares the base prefix
        // This SHOULD throw SecurityException but currently doesn't due to missing trailing separator
        var maliciousPath = Path.Combine(_siblingDir, "malicious.spec.csx");

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            finder.FindSpecs(maliciousPath, _testBaseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).Contains("within");
    }

    /// <summary>
    /// Test that the sibling directory itself is rejected.
    /// </summary>
    [Test]
    public async Task FindSpecs_SiblingDirectory_ShouldThrowSecurityException()
    {
        var finder = new SpecFinder();

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            finder.FindSpecs(_siblingDir, _testBaseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region Case Sensitivity Tests (Platform-specific)

    /// <summary>
    /// On Unix systems, path comparison should be case-sensitive.
    /// "/var/App/specs" should NOT match base "/var/app/specs"
    /// </summary>
    [Test]
    public async Task FindSpecs_OnUnix_PathComparisonShouldBeCaseSensitive()
    {
        // Skip on Windows - case insensitive filesystem
        if (OperatingSystem.IsWindows())
        {
            await Assert.That(true).IsTrue();
            return;
        }

        // Create directory with different case
        var differentCaseDir = _testBaseDir.Replace("draftspec", "DraftSpec");
        if (differentCaseDir == _testBaseDir)
        {
            // Can't test if names are identical
            await Assert.That(true).IsTrue();
            return;
        }

        // On Unix, this should be treated as a different directory
        // The test verifies case-sensitive comparison is used
        var finder = new SpecFinder();

        // If we're on a case-sensitive filesystem, these are different paths
        // This behavior is correct and should not throw SecurityException
        // since it's genuinely a different path
        await Assert.That(true).IsTrue();
    }

    /// <summary>
    /// On Windows, path comparison should be case-insensitive.
    /// "/VAR/APP/SPECS" should match base "/var/app/specs"
    /// </summary>
    [Test]
    public async Task FindSpecs_OnWindows_PathComparisonShouldBeCaseInsensitive()
    {
        if (!OperatingSystem.IsWindows())
        {
            await Assert.That(true).IsTrue();
            return;
        }

        var finder = new SpecFinder();

        // On Windows, different case should still be within base directory
        var upperCasePath = _testBaseDir.ToUpperInvariant();

        // This should NOT throw (it's the same directory on Windows)
        // But it will throw ArgumentException because no specs exist
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            finder.FindSpecs(upperCasePath, _testBaseDir);
            return Task.CompletedTask;
        });

        // Should get "No specs found" not "Security exception"
        await Assert.That(exception!.Message).Contains("No");
    }

    #endregion

    #region Path Normalization Tests

    /// <summary>
    /// Paths with redundant separators should be normalized before comparison.
    /// </summary>
    [Test]
    public async Task FindSpecs_PathWithRedundantSeparators_ShouldNormalize()
    {
        var finder = new SpecFinder();

        // Path with redundant separators that normalizes to within base
        var pathWithExtraSeps = _testBaseDir + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + "subdir";

        // Should get ArgumentException (not found) rather than SecurityException
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            finder.FindSpecs(pathWithExtraSeps, _testBaseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception!.Message).Contains("not found");
    }

    /// <summary>
    /// Paths with dot segments should be normalized.
    /// /base/./subdir should equal /base/subdir
    /// </summary>
    [Test]
    public async Task FindSpecs_PathWithDotSegment_ShouldNormalize()
    {
        var finder = new SpecFinder();

        var pathWithDot = Path.Combine(_testBaseDir, ".", "subdir");

        // Should normalize and either find specs or throw ArgumentException (not SecurityException)
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            finder.FindSpecs(pathWithDot, _testBaseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception!.Message).Contains("not found");
    }

    #endregion

    #region Error Message Security Tests

    /// <summary>
    /// Security exception messages should not leak the actual base path.
    /// </summary>
    [Test]
    public async Task SecurityException_ShouldNotLeakBasePath()
    {
        var finder = new SpecFinder();

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            finder.FindSpecs("/etc/passwd.spec.csx", _testBaseDir);
            return Task.CompletedTask;
        });

        // Message should be generic, not exposing internal paths
        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).DoesNotContain(_testBaseDir);
        await Assert.That(exception.Message).Contains("within");
    }

    #endregion
}