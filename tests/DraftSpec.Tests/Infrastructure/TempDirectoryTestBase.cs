namespace DraftSpec.Tests.Infrastructure;

/// <summary>
/// Base class for tests that require a temporary directory.
/// Creates a unique temp directory before each test and cleans it up after.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// public class MyTests : TempDirectoryTestBase
/// {
///     [Test]
///     public async Task MyTest()
///     {
///         var filePath = Path.Combine(TempDir, "test.txt");
///         File.WriteAllText(filePath, "content");
///         // ...
///     }
/// }
/// </code>
/// </remarks>
public abstract class TempDirectoryTestBase
{
    private string _tempDir = null!;

    /// <summary>
    /// Gets the path to the temporary directory for the current test.
    /// The directory is created fresh before each test and deleted after.
    /// </summary>
    protected string TempDir => _tempDir;

    /// <summary>
    /// Creates a unique temporary directory before each test.
    /// Directory name format: {TestClassName}_{Guid}
    /// </summary>
    [Before(Test)]
    public void CreateTempDirectory()
    {
        var className = GetType().Name;
        _tempDir = Path.Combine(Path.GetTempPath(), $"{className}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Cleans up the temporary directory after each test.
    /// Errors during cleanup are silently ignored to prevent test failures.
    /// </summary>
    [After(Test)]
    public void CleanupTempDirectory()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors to prevent test failures
            }
        }
    }
}
