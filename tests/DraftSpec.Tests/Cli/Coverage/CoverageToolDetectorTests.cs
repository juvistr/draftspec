using DraftSpec.Cli;
using DraftSpec.Cli.Coverage;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Coverage;

public class CoverageToolDetectorTests
{
    [Test]
    public async Task IsAvailable_WhenToolInstalled_ReturnsTrue()
    {
        // Arrange
        var mockRunner = new MockProcessRunner();
        mockRunner.AddResult(new ProcessResult("1.0.0", "", 0));
        var detector = new CoverageToolDetector(mockRunner);

        // Act
        var result = detector.IsAvailable;

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAvailable_WhenToolNotInstalled_ReturnsFalse()
    {
        // Arrange
        var mockRunner = new MockProcessRunner();
        mockRunner.AddResult(new ProcessResult("", "dotnet-coverage not found", 1));
        var detector = new CoverageToolDetector(mockRunner);

        // Act
        var result = detector.IsAvailable;

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAvailable_WhenProcessThrows_ReturnsFalse()
    {
        // Arrange
        var mockRunner = new MockProcessRunner { ThrowOnRun = true };
        var detector = new CoverageToolDetector(mockRunner);

        // Act
        var result = detector.IsAvailable;

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAvailable_CachesResult_OnlyCallsProcessOnce()
    {
        // Arrange
        var mockRunner = new MockProcessRunner();
        mockRunner.AddResult(new ProcessResult("1.0.0", "", 0));
        var detector = new CoverageToolDetector(mockRunner);

        // Act
        _ = detector.IsAvailable;
        _ = detector.IsAvailable;
        _ = detector.IsAvailable;

        // Assert
        await Assert.That(mockRunner.RunCalls).Count().IsEqualTo(1);
    }

    [Test]
    public async Task IsAvailable_CallsCorrectCommand()
    {
        // Arrange
        var mockRunner = new MockProcessRunner();
        mockRunner.AddResult(new ProcessResult("1.0.0", "", 0));
        var detector = new CoverageToolDetector(mockRunner);

        // Act
        _ = detector.IsAvailable;

        // Assert
        await Assert.That(mockRunner.RunCalls).Count().IsEqualTo(1);
        var (fileName, arguments) = mockRunner.RunCalls[0];
        await Assert.That(fileName).IsEqualTo("dotnet-coverage");
        await Assert.That(arguments).Contains("--version");
    }

    [Test]
    public async Task Version_WhenToolInstalled_ReturnsTrimmedVersion()
    {
        // Arrange
        var mockRunner = new MockProcessRunner();
        mockRunner.AddResult(new ProcessResult("1.2.3\n", "", 0));
        var detector = new CoverageToolDetector(mockRunner);

        // Act
        var result = detector.Version;

        // Assert
        await Assert.That(result).IsEqualTo("1.2.3");
    }

    [Test]
    public async Task Version_WhenToolNotInstalled_ReturnsNull()
    {
        // Arrange
        var mockRunner = new MockProcessRunner();
        mockRunner.AddResult(new ProcessResult("", "not found", 1));
        var detector = new CoverageToolDetector(mockRunner);

        // Act
        var result = detector.Version;

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Version_WithWhitespace_ReturnsTrimmedValue()
    {
        // Arrange
        var mockRunner = new MockProcessRunner();
        mockRunner.AddResult(new ProcessResult("  2.0.0  \r\n", "", 0));
        var detector = new CoverageToolDetector(mockRunner);

        // Act
        var result = detector.Version;

        // Assert
        await Assert.That(result).IsEqualTo("2.0.0");
    }

    [Test]
    public async Task Version_WhenProcessThrows_ReturnsNull()
    {
        // Arrange
        var mockRunner = new MockProcessRunner { ThrowOnRun = true };
        var detector = new CoverageToolDetector(mockRunner);

        // Act
        var result = detector.Version;

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Version_AccessBeforeIsAvailable_TriggersCheckAvailability()
    {
        // Arrange
        var mockRunner = new MockProcessRunner();
        mockRunner.AddResult(new ProcessResult("3.0.0", "", 0));
        var detector = new CoverageToolDetector(mockRunner);

        // Act - access Version first, before IsAvailable
        var version = detector.Version;
        var isAvailable = detector.IsAvailable;

        // Assert
        await Assert.That(mockRunner.RunCalls).Count().IsEqualTo(1);
        await Assert.That(version).IsEqualTo("3.0.0");
        await Assert.That(isAvailable).IsTrue();
    }

    [Test]
    public async Task Version_AfterIsAvailableCall_DoesNotCallProcessAgain()
    {
        // Arrange
        var mockRunner = new MockProcessRunner();
        mockRunner.AddResult(new ProcessResult("1.5.0", "", 0));
        var detector = new CoverageToolDetector(mockRunner);

        // Act
        _ = detector.IsAvailable;
        var version = detector.Version;

        // Assert
        await Assert.That(mockRunner.RunCalls).Count().IsEqualTo(1);
        await Assert.That(version).IsEqualTo("1.5.0");
    }

    [Test]
    public async Task Constructor_WithNullProcessRunner_UsesDefaultRunner()
    {
        // Arrange & Act
        var detector = new CoverageToolDetector(null);

        // Assert - just verify it doesn't throw
        await Assert.That(detector).IsNotNull();
    }

    [Test]
    public async Task Constructor_WithDefaultParameter_CreatesSuccessfully()
    {
        // Arrange & Act
        var detector = new CoverageToolDetector();

        // Assert - just verify it doesn't throw
        await Assert.That(detector).IsNotNull();
    }
}
