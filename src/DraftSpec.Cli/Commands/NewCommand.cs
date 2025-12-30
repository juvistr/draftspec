using DraftSpec.Cli.Options;

namespace DraftSpec.Cli.Commands;

public class NewCommand : ICommand<NewOptions>
{
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public NewCommand(IConsole console, IFileSystem fileSystem)
    {
        _console = console;
        _fileSystem = fileSystem;
    }

    public Task<int> ExecuteAsync(NewOptions options, CancellationToken ct = default)
    {
        var name = options.SpecName;
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Usage: draftspec new <Name>");

        // Security: Validate spec name doesn't contain path separators
        PathValidator.ValidateFileName(name);

        var directory = Path.GetFullPath(options.Path);

        if (!_fileSystem.DirectoryExists(directory))
            throw new ArgumentException($"Directory not found: {directory}");

        var specHelperPath = Path.Combine(directory, "spec_helper.csx");
        if (!_fileSystem.FileExists(specHelperPath))
        {
            _console.WriteWarning("Warning: spec_helper.csx not found. Run 'draftspec init' first.");
        }

        var specPath = Path.Combine(directory, $"{name}.spec.csx");
        if (_fileSystem.FileExists(specPath))
            throw new ArgumentException($"{name}.spec.csx already exists");

        var specContent = GenerateSpec(name);
        _fileSystem.WriteAllText(specPath, specContent);
        _console.WriteSuccess($"Created {name}.spec.csx");

        return Task.FromResult(0);
    }

    private static string GenerateSpec(string name)
    {
        return $$"""
                 #load "spec_helper.csx"
                 using static DraftSpec.Dsl;

                 describe("{{name}}", () => {
                     it("works"); // Pending spec - add implementation
                 });
                 """;
    }
}
