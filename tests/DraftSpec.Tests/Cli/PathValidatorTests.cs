using System.Security;
using DraftSpec.Cli;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for PathValidator to prevent path traversal attacks via filenames.
/// </summary>
public class PathValidatorTests
{
    private PathValidator CreateValidator(bool isWindows = false)
    {
        var os = isWindows ? new MockOperatingSystem().WithWindows() : new MockOperatingSystem();
        var pathComparer = new SystemPathComparer(os);
        return new PathValidator(pathComparer);
    }

    #region ValidateFileName Tests

    [Test]
    public async Task ValidateFileName_ValidName_DoesNotThrow()
    {
        var validator = CreateValidator();

        // Should not throw
        validator.ValidateFileName("MySpec");
        validator.ValidateFileName("my-spec");
        validator.ValidateFileName("my_spec_123");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ValidateFileName_EmptyName_ThrowsArgumentException()
    {
        var validator = CreateValidator();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            validator.ValidateFileName("");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ValidateFileName_NameWithForwardSlash_ThrowsArgumentException()
    {
        var validator = CreateValidator();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            validator.ValidateFileName("../../../etc/malicious");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).Contains("path separator");
    }

    [Test]
    public async Task ValidateFileName_NameWithBackslash_ThrowsArgumentException()
    {
        var validator = CreateValidator();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            validator.ValidateFileName("..\\..\\malicious");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ValidateFileName_DoubleDot_ThrowsArgumentException()
    {
        var validator = CreateValidator();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            validator.ValidateFileName("..");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).Contains("relative path");
    }

    [Test]
    public async Task ValidateFileName_SingleDot_ThrowsArgumentException()
    {
        var validator = CreateValidator();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            validator.ValidateFileName(".");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ValidateFileName_StartsWithDoubleDot_ThrowsArgumentException()
    {
        var validator = CreateValidator();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            validator.ValidateFileName("..foo");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region ValidatePathWithinBase Tests

    [Test]
    public async Task ValidatePathWithinBase_PathWithinBase_DoesNotThrow()
    {
        var validator = CreateValidator();
        var baseDir = Path.GetTempPath();
        var validPath = Path.Combine(baseDir, "subdir", "file.txt");

        // Should not throw
        validator.ValidatePathWithinBase(validPath, baseDir);

        await Task.CompletedTask;
    }

    [Test]
    public async Task ValidatePathWithinBase_ExactBaseDir_DoesNotThrow()
    {
        var validator = CreateValidator();
        var baseDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

        // The base directory itself should be valid
        validator.ValidatePathWithinBase(baseDir, baseDir);

        await Task.CompletedTask;
    }

    [Test]
    public async Task ValidatePathWithinBase_PathTraversal_ThrowsSecurityException()
    {
        var validator = CreateValidator();
        var baseDir = Path.Combine(Path.GetTempPath(), "sandbox");
        var escapePath = Path.Combine(baseDir, "..", "escaped.txt");

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            validator.ValidatePathWithinBase(escapePath, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).Contains("working directory");
    }

    [Test]
    public async Task ValidatePathWithinBase_AbsolutePathOutsideBase_ThrowsSecurityException()
    {
        var validator = CreateValidator();
        var baseDir = Path.Combine(Path.GetTempPath(), "allowed");
        var outsidePath = Path.Combine(Path.GetTempPath(), "notallowed", "file.txt");

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            validator.ValidatePathWithinBase(outsidePath, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ValidatePathWithinBase_PrefixAttack_ThrowsSecurityException()
    {
        var validator = CreateValidator();
        // Security test: "/var/app/specs-evil" should NOT be valid for base "/var/app/specs"
        var baseDir = Path.Combine(Path.GetTempPath(), "specs");
        var maliciousPath = Path.Combine(Path.GetTempPath(), "specs-evil", "payload.txt");

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            validator.ValidatePathWithinBase(maliciousPath, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ValidatePathWithinBase_NullBaseDir_UsesCurrentDirectory()
    {
        var validator = CreateValidator();
        // Get a path within the current directory
        var currentDir = Directory.GetCurrentDirectory();
        var validPath = Path.Combine(currentDir, "subdir", "file.txt");

        // Should not throw when baseDirectory is null (uses current dir)
        validator.ValidatePathWithinBase(validPath, null);

        await Task.CompletedTask;
    }

    [Test]
    public async Task ValidatePathWithinBase_RelativePath_ResolvesToAbsolute()
    {
        var validator = CreateValidator();
        var currentDir = Directory.GetCurrentDirectory();

        // Relative path that stays within current directory
        validator.ValidatePathWithinBase("./subdir/file.txt", currentDir);

        await Task.CompletedTask;
    }

    [Test]
    public async Task ValidatePathWithinBase_MultipleTraversals_ThrowsSecurityException()
    {
        var validator = CreateValidator();
        var baseDir = Path.Combine(Path.GetTempPath(), "deep", "nested", "sandbox");
        var escapePath = Path.Combine(baseDir, "..", "..", "..", "..", "escaped.txt");

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            validator.ValidatePathWithinBase(escapePath, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region OS-Specific Tests

    [Test]
    public async Task ValidatePathWithinBase_OnWindows_IsCaseInsensitive()
    {
        var validator = CreateValidator(isWindows: true);
        var baseDir = Path.Combine(Path.GetTempPath(), "MyBase");
        var validPath = Path.Combine(Path.GetTempPath(), "MYBASE", "file.txt");

        // On Windows, paths should be compared case-insensitively
        // The path should be valid since MYBASE == MyBase on Windows
        validator.ValidatePathWithinBase(validPath, baseDir);

        await Task.CompletedTask;
    }

    [Test]
    public async Task ValidatePathWithinBase_OnUnix_IsCaseSensitive()
    {
        var validator = CreateValidator(isWindows: false);
        var baseDir = Path.Combine(Path.GetTempPath(), "MyBase");
        var differentCasePath = Path.Combine(Path.GetTempPath(), "MYBASE", "file.txt");

        // On Unix, paths are case-sensitive so MYBASE != MyBase
        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            validator.ValidatePathWithinBase(differentCasePath, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    #endregion
}
