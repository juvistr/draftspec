using DraftSpec.Cli.Services;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Services;

/// <summary>
/// Tests for GitService.
/// </summary>
public class GitServiceTests
{
    #region File-Based Reference Tests

    [Test]
    public async Task GetChangedFilesAsync_WithFileReference_ReadsFileContents()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var changesFile = Path.Combine(tempDir, "changes.txt");
            await File.WriteAllTextAsync(changesFile, "file1.cs\nfile2.cs\nfile3.cs");

            var fileSystem = new MockFileSystem()
                .AddFile(changesFile, "file1.cs\nfile2.cs\nfile3.cs");

            var service = new GitService(fileSystem);

            // Act
            var result = await service.GetChangedFilesAsync(changesFile, tempDir);

            // Assert
            await Assert.That(result.Count).IsEqualTo(3);
            await Assert.That(result[0]).IsEqualTo(Path.Combine(tempDir, "file1.cs"));
            await Assert.That(result[1]).IsEqualTo(Path.Combine(tempDir, "file2.cs"));
            await Assert.That(result[2]).IsEqualTo(Path.Combine(tempDir, "file3.cs"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GetChangedFilesAsync_WithFileReference_TrimsWhitespace()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var changesFile = Path.Combine(tempDir, "changes.txt");
            var fileContent = "  file1.cs  \n\tfile2.cs\t\n  file3.cs  ";
            await File.WriteAllTextAsync(changesFile, fileContent);

            var fileSystem = new MockFileSystem()
                .AddFile(changesFile, fileContent);

            var service = new GitService(fileSystem);

            // Act
            var result = await service.GetChangedFilesAsync(changesFile, tempDir);

            // Assert
            await Assert.That(result.Count).IsEqualTo(3);
            await Assert.That(result[0]).IsEqualTo(Path.Combine(tempDir, "file1.cs"));
            await Assert.That(result[1]).IsEqualTo(Path.Combine(tempDir, "file2.cs"));
            await Assert.That(result[2]).IsEqualTo(Path.Combine(tempDir, "file3.cs"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GetChangedFilesAsync_WithFileReference_FiltersEmptyLines()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var changesFile = Path.Combine(tempDir, "changes.txt");
            var fileContent = "file1.cs\n\n\nfile2.cs\n\n";
            await File.WriteAllTextAsync(changesFile, fileContent);

            var fileSystem = new MockFileSystem()
                .AddFile(changesFile, fileContent);

            var service = new GitService(fileSystem);

            // Act
            var result = await service.GetChangedFilesAsync(changesFile, tempDir);

            // Assert
            await Assert.That(result.Count).IsEqualTo(2);
            await Assert.That(result[0]).IsEqualTo(Path.Combine(tempDir, "file1.cs"));
            await Assert.That(result[1]).IsEqualTo(Path.Combine(tempDir, "file2.cs"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GetChangedFilesAsync_WithFileReference_NormalizesToAbsolutePaths()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var changesFile = Path.Combine(tempDir, "changes.txt");
            var fileContent = "./src/file1.cs\n../other/file2.cs";
            await File.WriteAllTextAsync(changesFile, fileContent);

            var fileSystem = new MockFileSystem()
                .AddFile(changesFile, fileContent);

            var service = new GitService(fileSystem);

            // Act
            var result = await service.GetChangedFilesAsync(changesFile, tempDir);

            // Assert
            await Assert.That(result.Count).IsEqualTo(2);
            await Assert.That(Path.IsPathFullyQualified(result[0])).IsTrue();
            await Assert.That(Path.IsPathFullyQualified(result[1])).IsTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GetChangedFilesAsync_WithEmptyFile_ReturnsEmptyList()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var changesFile = Path.Combine(tempDir, "changes.txt");
            await File.WriteAllTextAsync(changesFile, "");

            var fileSystem = new MockFileSystem()
                .AddFile(changesFile, "");

            var service = new GitService(fileSystem);

            // Act
            var result = await service.GetChangedFilesAsync(changesFile, tempDir);

            // Assert
            await Assert.That(result.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region Git Integration Tests

    [Test]
    public async Task IsGitRepositoryAsync_InActualGitRepo_ReturnsTrue()
    {
        // Arrange - Use the draftspec repo itself as a test subject
        var repoDir = GetRepositoryRoot();
        var fileSystem = new RealFileSystem();
        var service = new GitService(fileSystem);

        // Act
        var result = await service.IsGitRepositoryAsync(repoDir);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsGitRepositoryAsync_OutsideGitRepo_ReturnsFalse()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var fileSystem = new RealFileSystem();
            var service = new GitService(fileSystem);

            // Act
            var result = await service.IsGitRepositoryAsync(tempDir);

            // Assert
            await Assert.That(result).IsFalse();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GetChangedFilesAsync_WithStagedReference_RunsGitDiffCached()
    {
        // Arrange - Use the draftspec repo itself
        var repoDir = GetRepositoryRoot();
        var fileSystem = new RealFileSystem();
        var service = new GitService(fileSystem);

        // Act - Just verify it doesn't throw for "staged"
        // (result depends on current git state)
        var result = await service.GetChangedFilesAsync("staged", repoDir);

        // Assert - Should return a list (possibly empty)
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task GetChangedFilesAsync_WithCommitRef_RunsGitDiff()
    {
        // Arrange - Use the draftspec repo itself
        var repoDir = GetRepositoryRoot();
        var fileSystem = new RealFileSystem();
        var service = new GitService(fileSystem);

        // Act - Get changes between two recent commits
        // HEAD~1..HEAD should return files that changed in the last commit
        var result = await service.GetChangedFilesAsync("HEAD~1", repoDir);

        // Assert - Should return a list of files (may vary based on repo state)
        await Assert.That(result).IsNotNull();
        // All returned paths should be absolute
        foreach (var path in result)
        {
            await Assert.That(Path.IsPathFullyQualified(path)).IsTrue();
        }
    }

    [Test]
    public async Task GetChangedFilesAsync_WithInvalidCommitRef_ThrowsWithErrorMessage()
    {
        // Arrange
        var repoDir = GetRepositoryRoot();
        var fileSystem = new RealFileSystem();
        var service = new GitService(fileSystem);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GetChangedFilesAsync("not-a-real-commit-ref-12345", repoDir));
    }

    #endregion

    #region Helper Methods

    private static string GetRepositoryRoot()
    {
        // Navigate up from test directory to find repository root
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException("Could not find repository root");
    }

    #endregion
}
