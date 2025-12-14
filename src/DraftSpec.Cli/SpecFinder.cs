namespace DraftSpec.Cli;

public class SpecFinder
{
    public IReadOnlyList<string> FindSpecs(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (File.Exists(fullPath))
        {
            if (!fullPath.EndsWith(".spec.csx", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"File must end with .spec.csx: {path}");
            return [fullPath];
        }

        if (Directory.Exists(fullPath))
        {
            var specs = Directory.GetFiles(fullPath, "*.spec.csx", SearchOption.AllDirectories)
                .OrderBy(f => f)
                .ToList();

            if (specs.Count == 0)
                throw new ArgumentException($"No *.spec.csx files found in: {path}");

            return specs;
        }

        throw new ArgumentException($"Path not found: {path}");
    }
}
