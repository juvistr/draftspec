using System.Security;
using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for path validation security measures.
/// </summary>
public class PathValidationTests
{
    #region Output Path Validation Tests

    [Test]
    public async Task OutputPath_WithinCurrentDirectory_IsAllowed()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var outputPath = Path.Combine(currentDir, "output.json");
        var fullPath = Path.GetFullPath(outputPath);

        // Validation logic from RunCommand
        var isValid = fullPath.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase);

        await Assert.That(isValid).IsTrue();
    }

    [Test]
    public async Task OutputPath_InSubdirectory_IsAllowed()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var outputPath = Path.Combine(currentDir, "reports", "output.json");
        var fullPath = Path.GetFullPath(outputPath);

        var isValid = fullPath.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase);

        await Assert.That(isValid).IsTrue();
    }

    [Test]
    public async Task OutputPath_RelativeWithinCurrent_IsAllowed()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var outputPath = "./output.json";
        var fullPath = Path.GetFullPath(outputPath);

        var isValid = fullPath.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase);

        await Assert.That(isValid).IsTrue();
    }

    [Test]
    public async Task OutputPath_ParentDirectory_IsRejected()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var outputPath = "../output.json";
        var fullPath = Path.GetFullPath(outputPath);

        var isValid = fullPath.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase);

        await Assert.That(isValid).IsFalse();
    }

    [Test]
    public async Task OutputPath_AbsoluteOutsideCurrent_IsRejected()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var outputPath = "/tmp/output.json";
        var fullPath = Path.GetFullPath(outputPath);

        var isValid = fullPath.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase);

        await Assert.That(isValid).IsFalse();
    }

    [Test]
    public async Task OutputPath_TraversalAttempt_IsRejected()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var outputPath = "subdir/../../etc/passwd";
        var fullPath = Path.GetFullPath(outputPath);

        var isValid = fullPath.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase);

        await Assert.That(isValid).IsFalse();
    }

    #endregion

    #region SpecFinder Path Validation Tests

    [Test]
    public async Task SpecFinder_PathOutsideBase_ThrowsSecurityException()
    {
        var finder = new SpecFinder();
        var baseDir = Directory.GetCurrentDirectory();

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            finder.FindSpecs("../outside.spec.csx", baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task SpecFinder_AbsolutePathOutsideBase_ThrowsSecurityException()
    {
        var finder = new SpecFinder();
        var baseDir = Directory.GetCurrentDirectory();

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            finder.FindSpecs("/tmp/malicious.spec.csx", baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task SpecFinder_TraversalInPath_ThrowsSecurityException()
    {
        var finder = new SpecFinder();
        var baseDir = Directory.GetCurrentDirectory();

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            finder.FindSpecs("valid/../../../etc/passwd.spec.csx", baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task SpecFinder_PathWithinBase_ThrowsArgumentExceptionNotSecurity()
    {
        var finder = new SpecFinder();
        var baseDir = Directory.GetCurrentDirectory();

        // This should throw ArgumentException (file not found) not SecurityException
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            finder.FindSpecs("nonexistent.spec.csx", baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception!.Message).Contains("not found");
    }

    #endregion

    #region Security Exception Messages

    [Test]
    public async Task SecurityException_ContainsHelpfulMessage()
    {
        var finder = new SpecFinder();
        var baseDir = Directory.GetCurrentDirectory();

        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
        {
            finder.FindSpecs("../escape.spec.csx", baseDir);
            return Task.CompletedTask;
        });

        await Assert.That(exception!.Message).Contains("within");
    }

    #endregion
}