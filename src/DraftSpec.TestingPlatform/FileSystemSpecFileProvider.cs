namespace DraftSpec.TestingPlatform;

/// <summary>
/// Default implementation of ISpecFileProvider that uses the file system.
/// </summary>
public class FileSystemSpecFileProvider : ISpecFileProvider
{
    /// <inheritdoc />
    public IEnumerable<string> GetSpecFiles(string directory)
    {
        return Directory.EnumerateFiles(directory, "*.spec.csx", SearchOption.AllDirectories);
    }

    /// <inheritdoc />
    public string GetRelativePath(string basePath, string absolutePath)
    {
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
        return File.Exists(path);
    }
}
