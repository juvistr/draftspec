using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for PathValidator to prevent path traversal attacks via filenames.
/// </summary>
public class PathValidatorTests
{
    #region ValidateFileName Tests

    [Test]
    public async Task ValidateFileName_ValidName_DoesNotThrow()
    {
        // Should not throw
        PathValidator.ValidateFileName("MySpec");
        PathValidator.ValidateFileName("my-spec");
        PathValidator.ValidateFileName("my_spec_123");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ValidateFileName_EmptyName_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            PathValidator.ValidateFileName("");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ValidateFileName_NameWithForwardSlash_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            PathValidator.ValidateFileName("../../../etc/malicious");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).Contains("path separator");
    }

    [Test]
    public async Task ValidateFileName_NameWithBackslash_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            PathValidator.ValidateFileName("..\\..\\malicious");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ValidateFileName_DoubleDot_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            PathValidator.ValidateFileName("..");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).Contains("relative path");
    }

    [Test]
    public async Task ValidateFileName_SingleDot_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            PathValidator.ValidateFileName(".");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ValidateFileName_StartsWithDoubleDot_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            PathValidator.ValidateFileName("..foo");
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region ValidatePathWithinBase Tests

    [Test]
    public async Task ValidatePathWithinBase_PathWithinBase_DoesNotThrow()
    {
        var baseDir = Path.GetTempPath();
        var validPath = Path.Combine(baseDir, "subdir", "file.txt");

        // Should not throw
        PathValidator.ValidatePathWithinBase(validPath, baseDir);

        await Task.CompletedTask;
    }

    [Test]
    public async Task ValidatePathWithinBase_ExactBaseDir_DoesNotThrow()
    {
        var baseDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

        // The base directory itself should be valid
        PathValidator.ValidatePathWithinBase(baseDir, baseDir);

        await Task.CompletedTask;
    }

    [Test]
    public async Task ValidatePathWithinBase_PathTraversal_ThrowsSecurityException()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "sandbox");
        var escapePath = Path.Combine(baseDir, "..", "escaped.txt");

        var exception = await Assert.ThrowsAsync<System.Security.SecurityException>(() =>
        {
            PathValidator.ValidatePathWithinBase(escapePath, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).Contains("working directory");
    }

    [Test]
    public async Task ValidatePathWithinBase_AbsolutePathOutsideBase_ThrowsSecurityException()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "allowed");
        var outsidePath = Path.Combine(Path.GetTempPath(), "notallowed", "file.txt");

        var exception = await Assert.ThrowsAsync<System.Security.SecurityException>(() =>
        {
            PathValidator.ValidatePathWithinBase(outsidePath, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ValidatePathWithinBase_PrefixAttack_ThrowsSecurityException()
    {
        // Security test: "/var/app/specs-evil" should NOT be valid for base "/var/app/specs"
        var baseDir = Path.Combine(Path.GetTempPath(), "specs");
        var maliciousPath = Path.Combine(Path.GetTempPath(), "specs-evil", "payload.txt");

        var exception = await Assert.ThrowsAsync<System.Security.SecurityException>(() =>
        {
            PathValidator.ValidatePathWithinBase(maliciousPath, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task ValidatePathWithinBase_NullBaseDir_UsesCurrentDirectory()
    {
        // Get a path within the current directory
        var currentDir = Directory.GetCurrentDirectory();
        var validPath = Path.Combine(currentDir, "subdir", "file.txt");

        // Should not throw when baseDirectory is null (uses current dir)
        PathValidator.ValidatePathWithinBase(validPath, null);

        await Task.CompletedTask;
    }

    [Test]
    public async Task ValidatePathWithinBase_RelativePath_ResolvesToAbsolute()
    {
        var currentDir = Directory.GetCurrentDirectory();

        // Relative path that stays within current directory
        PathValidator.ValidatePathWithinBase("./subdir/file.txt", currentDir);

        await Task.CompletedTask;
    }

    [Test]
    public async Task ValidatePathWithinBase_MultipleTraversals_ThrowsSecurityException()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "deep", "nested", "sandbox");
        var escapePath = Path.Combine(baseDir, "..", "..", "..", "..", "escaped.txt");

        var exception = await Assert.ThrowsAsync<System.Security.SecurityException>(() =>
        {
            PathValidator.ValidatePathWithinBase(escapePath, baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    #endregion
}
