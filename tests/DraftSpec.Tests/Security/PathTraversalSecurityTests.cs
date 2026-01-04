using System.Security;
using DraftSpec.Abstractions;
using DraftSpec.Cli;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Security;

// Note: These tests use a helper that automatically selects the correct path comparer
// based on the actual runtime OS, since the tests check OS-specific path behavior.

/// <summary>
/// Unit tests for path traversal security (CWE-22) in SpecFinder.
/// Uses mocked file system to test path validation logic in isolation.
/// </summary>
public class PathTraversalSecurityTests
{
    #region Prefix Bypass Attack Tests

    /// <summary>
    /// Sibling directory with same prefix should be rejected.
    /// Attack: base="/var/app/specs" evil="/var/app/specs-evil/malicious.spec.csx"
    /// Without trailing separator: "/var/app/specs-evil".StartsWith("/var/app/specs") = TRUE (VULNERABLE)
    /// With trailing separator: "/var/app/specs-evil".StartsWith("/var/app/specs/") = FALSE (SECURE)
    /// </summary>
    [Test]
    public async Task FindSpecs_SiblingDirectoryWithSamePrefix_ThrowsSecurityException()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "specs");
        var siblingDir = Path.Combine(Path.GetTempPath(), "specs-evil");
        var maliciousFile = Path.Combine(siblingDir, "malicious.spec.csx");

        var mockFs = new MockFileSystem();
        mockFs.AddFile(maliciousFile);

        var finder = new SpecFinder(mockFs, new SystemPathComparer(new SystemOperatingSystem()));

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            finder.FindSpecs(maliciousFile, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).Contains("within");
    }

    [Test]
    public async Task FindSpecs_SiblingDirectory_ThrowsSecurityException()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "myapp");
        var siblingDir = Path.Combine(Path.GetTempPath(), "myapp-compromised");

        var mockFs = new MockFileSystem();
        mockFs.AddDirectory(siblingDir);
        mockFs.AddFilesInDirectory(siblingDir, ["test.spec.csx"]);

        var finder = new SpecFinder(mockFs, new SystemPathComparer(new SystemOperatingSystem()));

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            finder.FindSpecs(siblingDir, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region Case Sensitivity Tests

    [Test]
    public async Task FindSpecs_OnUnix_PathComparisonIsCaseSensitive()
    {
        if (OperatingSystem.IsWindows()) return;

        var baseDir = "/tmp/draftspec-test";
        var differentCaseDir = "/tmp/DraftSpec-Test";

        var mockFs = new MockFileSystem();
        mockFs.AddDirectory(differentCaseDir);

        var finder = new SpecFinder(mockFs, new SystemPathComparer(new SystemOperatingSystem()));

        // On Unix with case-sensitive comparison, these are different directories
        // so differentCaseDir should be rejected as outside baseDir
        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            finder.FindSpecs(differentCaseDir, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task FindSpecs_OnWindows_PathComparisonIsCaseInsensitive()
    {
        if (!OperatingSystem.IsWindows()) return;

        var baseDir = @"C:\Projects\specs";
        var upperCasePath = @"C:\PROJECTS\SPECS";

        var mockFs = new MockFileSystem();
        mockFs.AddDirectory(upperCasePath);

        var finder = new SpecFinder(mockFs, new SystemPathComparer(new SystemOperatingSystem()));

        // On Windows, different case should still be within base directory
        // Should return empty list (no specs), not throw SecurityException
        var result = finder.FindSpecs(upperCasePath, baseDir);

        await Assert.That(result).IsEmpty();
    }

    #endregion

    #region Path Normalization Tests

    [Test]
    public async Task FindSpecs_PathWithDotDot_ThrowsSecurityException()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "safe", "specs");
        var escapePath = Path.Combine(baseDir, "..", "..", "etc", "passwd.spec.csx");

        var mockFs = new MockFileSystem();
        var finder = new SpecFinder(mockFs, new SystemPathComparer(new SystemOperatingSystem()));

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            finder.FindSpecs(escapePath, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task FindSpecs_PathWithDotSegment_Normalizes()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "base");
        var pathWithDot = Path.Combine(baseDir, ".", "subdir");

        var mockFs = new MockFileSystem();
        var finder = new SpecFinder(mockFs, new SystemPathComparer(new SystemOperatingSystem()));

        // Path normalizes to within base, but doesn't exist
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            finder.FindSpecs(pathWithDot, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception!.Message).Contains("not found");
    }

    #endregion

    #region Error Message Security Tests

    [Test]
    public async Task SecurityException_DoesNotLeakBasePath()
    {
        var baseDir = "/secret/internal/path/specs";
        var attackPath = "/etc/passwd.spec.csx";

        var mockFs = new MockFileSystem();
        var finder = new SpecFinder(mockFs, new SystemPathComparer(new SystemOperatingSystem()));

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            finder.FindSpecs(attackPath, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).DoesNotContain(baseDir);
        await Assert.That(exception.Message).Contains("within");
    }

    #endregion

    #region Valid Path Tests

    [Test]
    public async Task FindSpecs_ValidFileWithinBase_ReturnsFile()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "project");
        var specFile = Path.Combine(baseDir, "test.spec.csx");

        var mockFs = new MockFileSystem();
        mockFs.AddFile(specFile);

        var finder = new SpecFinder(mockFs, new SystemPathComparer(new SystemOperatingSystem()));

        var result = finder.FindSpecs(specFile, baseDir);

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0]).IsEqualTo(specFile);
    }

    [Test]
    public async Task FindSpecs_ValidDirectoryWithinBase_ReturnsSpecs()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "project");
        var specsDir = Path.Combine(baseDir, "specs");

        var mockFs = new MockFileSystem();
        mockFs.AddDirectory(specsDir);
        mockFs.AddFilesInDirectory(specsDir, ["one.spec.csx", "two.spec.csx"]);

        var finder = new SpecFinder(mockFs, new SystemPathComparer(new SystemOperatingSystem()));

        var result = finder.FindSpecs(specsDir, baseDir);

        await Assert.That(result).Count().IsEqualTo(2);
    }

    [Test]
    public async Task FindSpecs_NestedDirectoryWithinBase_ReturnsSpecs()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "project");
        var nestedDir = Path.Combine(baseDir, "src", "tests", "specs");

        var mockFs = new MockFileSystem();
        mockFs.AddDirectory(nestedDir);
        mockFs.AddFilesInDirectory(nestedDir, ["nested.spec.csx"]);

        var finder = new SpecFinder(mockFs, new SystemPathComparer(new SystemOperatingSystem()));

        var result = finder.FindSpecs(nestedDir, baseDir);

        await Assert.That(result).Count().IsEqualTo(1);
    }

    #endregion
}
