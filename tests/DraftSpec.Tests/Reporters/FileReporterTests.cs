using System.Security;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.Plugins.Reporters;

namespace DraftSpec.Tests.Reporters;

/// <summary>
/// Tests for FileReporter including path validation security.
/// </summary>
public class FileReporterTests
{
    private string _testDirectory = null!;

    [Before(Test)]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileReporterTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, true);
    }

    #region Path Validation

    [Test]
    public async Task Constructor_ValidPath_Succeeds()
    {
        var filePath = Path.Combine(_testDirectory, "report.json");

        var reporter = new FileReporter(filePath, new JsonFormatter(), _testDirectory);

        await Assert.That(reporter.Name).IsEqualTo("file:report.json");
    }

    [Test]
    public async Task Constructor_ValidSubdirectoryPath_Succeeds()
    {
        var filePath = Path.Combine(_testDirectory, "subdir", "report.json");

        var reporter = new FileReporter(filePath, new JsonFormatter(), _testDirectory);

        await Assert.That(reporter.Name).IsEqualTo("file:report.json");
    }

    [Test]
    public async Task Constructor_PathTraversal_ThrowsSecurityException()
    {
        var filePath = Path.Combine(_testDirectory, "..", "escaped.json");

        await Assert.That(() => new FileReporter(filePath, new JsonFormatter(), _testDirectory))
            .Throws<SecurityException>();
    }

    [Test]
    public async Task Constructor_AbsolutePathOutsideAllowed_ThrowsSecurityException()
    {
        var outsidePath = Path.Combine(Path.GetTempPath(), "other_directory", "report.json");

        await Assert.That(() => new FileReporter(outsidePath, new JsonFormatter(), _testDirectory))
            .Throws<SecurityException>();
    }

    [Test]
    public async Task Constructor_PrefixBypass_ThrowsSecurityException()
    {
        // Create a sibling directory that starts with the same prefix
        var siblingDir = _testDirectory + "-evil";
        Directory.CreateDirectory(siblingDir);

        try
        {
            var filePath = Path.Combine(siblingDir, "report.json");

            // This should fail because "testdir-evil" is not within "testdir"
            await Assert.That(() => new FileReporter(filePath, new JsonFormatter(), _testDirectory))
                .Throws<SecurityException>();
        }
        finally
        {
            if (Directory.Exists(siblingDir)) Directory.Delete(siblingDir, true);
        }
    }

    [Test]
    public async Task Constructor_WithCustomAllowedDirectory_Validates()
    {
        var customAllowed = Path.Combine(_testDirectory, "allowed");
        Directory.CreateDirectory(customAllowed);

        var validPath = Path.Combine(customAllowed, "report.json");
        var invalidPath = Path.Combine(_testDirectory, "report.json");

        // Valid path within custom allowed directory
        var reporter = new FileReporter(validPath, new JsonFormatter(), customAllowed);
        await Assert.That(reporter).IsNotNull();

        // Invalid path outside custom allowed directory
        await Assert.That(() => new FileReporter(invalidPath, new JsonFormatter(), customAllowed))
            .Throws<SecurityException>();
    }

    [Test]
    public async Task Constructor_DefaultAllowedDirectory_UsesCurrentDirectory()
    {
        // When no allowed directory is specified, it should default to current directory
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);
            var currentDir = Directory.GetCurrentDirectory(); // Get resolved path (handles symlinks)

            // Path within the current directory should succeed
            var filePath = Path.Combine(currentDir, "report.json");
            var reporter = new FileReporter(filePath, new JsonFormatter());

            await Assert.That(reporter).IsNotNull();

            // Path outside current directory should fail (use a completely different location)
            var outsideDir = Path.Combine(currentDir, "..", "definitely-outside-" + Guid.NewGuid().ToString("N"));
            var outsidePath = Path.Combine(outsideDir, "report.json");
            await Assert.That(() => new FileReporter(outsidePath, new JsonFormatter()))
                .Throws<SecurityException>();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    #endregion

    #region Functionality

    [Test]
    public async Task OnRunCompleted_WritesFormattedContent()
    {
        var filePath = Path.Combine(_testDirectory, "report.json");
        var reporter = new FileReporter(filePath, new JsonFormatter(), _testDirectory);

        var report = CreateReport();
        await reporter.OnRunCompletedAsync(report);

        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("\"total\":");
        await Assert.That(content).Contains("\"passed\":");
    }

    [Test]
    public async Task OnRunCompleted_CreatesDirectoryIfNotExists()
    {
        var subDir = Path.Combine(_testDirectory, "nested", "subdir");
        var filePath = Path.Combine(subDir, "report.json");
        var reporter = new FileReporter(filePath, new JsonFormatter(), _testDirectory);

        await Assert.That(Directory.Exists(subDir)).IsFalse();

        await reporter.OnRunCompletedAsync(CreateReport());

        await Assert.That(Directory.Exists(subDir)).IsTrue();
        await Assert.That(File.Exists(filePath)).IsTrue();
    }

    [Test]
    public async Task Name_ReturnsFilePrefix()
    {
        var filePath = Path.Combine(_testDirectory, "my-report.html");
        var reporter = new FileReporter(filePath, new JsonFormatter(), _testDirectory);

        await Assert.That(reporter.Name).IsEqualTo("file:my-report.html");
    }

    #endregion

    #region Constructor Validation

    [Test]
    public async Task Constructor_NullPath_ThrowsArgumentException()
    {
        await Assert.That(() => new FileReporter(null!, new JsonFormatter(), _testDirectory))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_EmptyPath_ThrowsArgumentException()
    {
        await Assert.That(() => new FileReporter("", new JsonFormatter(), _testDirectory))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_WhitespacePath_ThrowsArgumentException()
    {
        await Assert.That(() => new FileReporter("   ", new JsonFormatter(), _testDirectory))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_NullFormatter_ThrowsArgumentNullException()
    {
        var filePath = Path.Combine(_testDirectory, "report.json");

        await Assert.That(() => new FileReporter(filePath, null!, _testDirectory))
            .Throws<ArgumentNullException>();
    }

    #endregion

    #region Helper Methods

    private static SpecReport CreateReport()
    {
        return new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 1, Passed = 1 },
            Contexts = []
        };
    }

    #endregion
}