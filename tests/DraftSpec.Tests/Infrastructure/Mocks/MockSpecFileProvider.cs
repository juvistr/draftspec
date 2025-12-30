using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of ISpecFileProvider for testing.
/// </summary>
public class MockSpecFileProvider : ISpecFileProvider
{
    private readonly Dictionary<string, List<string>> _specFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _existingFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tracks calls to GetSpecFiles for verification.
    /// </summary>
    public List<string> GetSpecFilesCalls { get; } = [];

    /// <summary>
    /// Tracks calls to FileExists for verification.
    /// </summary>
    public List<string> FileExistsCalls { get; } = [];

    /// <summary>
    /// Configures the mock to return specific spec files for a directory.
    /// </summary>
    public MockSpecFileProvider WithSpecFiles(string directory, params string[] files)
    {
        _specFiles[directory] = new List<string>(files);
        foreach (var file in files)
        {
            _existingFiles.Add(file);
        }
        return this;
    }

    /// <summary>
    /// Configures the mock to report that a file exists.
    /// </summary>
    public MockSpecFileProvider WithExistingFile(string path)
    {
        _existingFiles.Add(path);
        return this;
    }

    /// <summary>
    /// Configures the mock to return no spec files for any directory.
    /// </summary>
    public MockSpecFileProvider WithNoSpecFiles()
    {
        _specFiles.Clear();
        return this;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetSpecFiles(string directory)
    {
        GetSpecFilesCalls.Add(directory);

        if (_specFiles.TryGetValue(directory, out var files))
        {
            return files;
        }

        return [];
    }

    /// <inheritdoc />
    public string GetRelativePath(string basePath, string absolutePath)
    {
        // Simple implementation: just use Path.GetRelativePath
        return Path.GetRelativePath(basePath, absolutePath);
    }

    /// <inheritdoc />
    public string GetAbsolutePath(string basePath, string relativePath)
    {
        return Path.GetFullPath(relativePath, basePath);
    }

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        FileExistsCalls.Add(path);
        return _existingFiles.Contains(path);
    }
}
