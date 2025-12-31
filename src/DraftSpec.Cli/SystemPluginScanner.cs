namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Default implementation that scans the actual file system.
/// </summary>
public class SystemPluginScanner : IPluginScanner
{
    public bool DirectoryExists(string directory) => Directory.Exists(directory);

    public IEnumerable<string> FindPluginFiles(string directory)
        => Directory.GetFiles(directory, "DraftSpec.*.dll");
}
