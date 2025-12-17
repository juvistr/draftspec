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
}
