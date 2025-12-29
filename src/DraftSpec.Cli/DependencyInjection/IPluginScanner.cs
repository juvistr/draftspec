namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Abstraction for scanning plugin directories and files.
/// </summary>
public interface IPluginScanner
{
    /// <summary>
    /// Checks if a directory exists.
    /// </summary>
    bool DirectoryExists(string directory);

    /// <summary>
    /// Finds all plugin DLL files in the specified directory.
    /// </summary>
    IEnumerable<string> FindPluginFiles(string directory);
}

/// <summary>
/// Default implementation that scans the actual file system.
/// </summary>
public class SystemPluginScanner : IPluginScanner
{
    public bool DirectoryExists(string directory) => Directory.Exists(directory);

    public IEnumerable<string> FindPluginFiles(string directory)
        => Directory.GetFiles(directory, "DraftSpec.*.dll");
}
